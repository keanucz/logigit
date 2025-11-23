package com.github.keanucz.logigit.git

import com.github.keanucz.logigit.ipc.LogiGitMessage
import com.github.keanucz.logigit.ipc.LogiGitTelemetry
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.application.ModalityState
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.ProjectManager
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.vcs.ProjectLevelVcsManager
import com.intellij.openapi.vcs.changes.ChangeListManager
import com.intellij.util.concurrency.AppExecutorUtil
import kotlinx.serialization.json.booleanOrNull
import kotlinx.serialization.json.intOrNull
import kotlinx.serialization.json.jsonPrimitive
import java.io.BufferedReader
import java.io.InputStreamReader
import java.nio.charset.StandardCharsets
import java.time.Instant
import java.util.concurrent.ExecutorService

class LogiGitCommandExecutor(
    private val telemetry: LogiGitTelemetry = LogiGitTelemetry.instance(),
    private val gitRunner: GitRunner = GitRunner.cli(),
    private val worker: ExecutorService = AppExecutorUtil.createBoundedApplicationPoolExecutor("LogiGit-GitExecutor", 2)
) {
    private val logger = Logger.getInstance(LogiGitCommandExecutor::class.java)

    fun handleIntent(message: LogiGitMessage) {
        val command = message.payload["command"]?.jsonPrimitive?.content ?: return
        val parameter = message.payload["parameter"]?.jsonPrimitive?.content
        val requiresClean = message.payload["requiresCleanRepo"]?.jsonPrimitive?.booleanOrNull ?: false
        val project = ProjectManager.getInstance().openProjects.firstOrNull()
        if (project == null) {
            telemetry.event("git.intent.denied", message.correlationId, mapOf("command" to command, "reason" to "noProject"))
            return
        }

        val repositoryRoot = resolveGitRoot(project)
        if (repositoryRoot == null) {
            telemetry.event("git.intent.denied", message.correlationId, mapOf("command" to command, "reason" to "noRepo"))
            return
        }

        if (requiresClean && !isRepoClean(project)) {
            telemetry.event("git.intent.denied", message.correlationId, mapOf("command" to command, "reason" to "dirty"))
            logger.warn("Rejecting $command because repository has local changes")
            return
        }

        telemetry.event("git.intent.accepted", message.correlationId, mapOf("command" to command))
        worker.submit {
            val result = gitRunner.run(repositoryRoot.path, command, parameter)
            telemetry.event(
                "git.intent.result",
                message.correlationId,
                mapOf(
                    "command" to command,
                    "exitCode" to result.exitCode,
                    "stdout" to result.stdout,
                    "stderr" to result.stderr
                )
            )
        }
    }

    fun handleScroll(message: LogiGitMessage) {
        val ticks = message.payload["ticks"]?.jsonPrimitive?.intOrNull ?: return
        val project = ProjectManager.getInstance().openProjects.firstOrNull() ?: return
        val editor = FileEditorManager.getInstance(project).selectedTextEditor ?: return
        ApplicationManager.getApplication().invokeLater({
            val scrollModel = editor.scrollingModel
            val delta = -ticks * editor.lineHeight
            scrollModel.scrollVertically(scrollModel.verticalScrollOffset + delta)
        }, ModalityState.NON_MODAL)
        telemetry.event("dial.scroll.applied", message.correlationId, mapOf("ticks" to ticks))
    }

    fun handleGesture(message: LogiGitMessage) {
        val gesture = message.payload["gesture"]?.jsonPrimitive?.content ?: "unknown"
        telemetry.event("gesture.received", message.correlationId, mapOf("gesture" to gesture))
    }

    protected open fun resolveGitRoot(project: Project): VirtualFile? {
        val vcsManager = ProjectLevelVcsManager.getInstance(project)
        return vcsManager.allVcsRoots.firstOrNull { it.vcs?.name.equals("Git", ignoreCase = true) }?.path
    }

    protected open fun isRepoClean(project: Project): Boolean =
        ChangeListManager.getInstance(project).allChanges.isEmpty()

    companion object {
        fun instance(): LogiGitCommandExecutor = ApplicationManager.getApplication().getService(LogiGitCommandExecutor::class.java)
    }

    data class GitCliResult(val exitCode: Int, val stdout: String, val stderr: String)

    fun interface GitRunner {
        fun run(rootPath: String, command: String, parameter: String?): GitCliResult

        companion object {
            fun cli(): GitRunner = GitRunner { rootPath, command, parameter ->
                val args = when (command) {
                    "git.stash" -> listOf("stash", "push", "-m", parameter.takeUnless { it.isNullOrBlank() } ?: "logigit-${Instant.now()}")
                    "git.stash.pop" -> listOf("stash", "pop")
                    "git.reset.head" -> listOf("reset", "--hard", "HEAD~1")
                    "git.push" -> listOf("push")
                    "git.pull" -> listOf("pull")
                    "git.status" -> listOf("status", "-sb")
                    "git.checkout" -> parameter?.let { listOf("checkout", it) }
                    "git.log" -> listOf("log", "-5", "--oneline")
                    else -> null
                } ?: return@GitRunner GitCliResult(1, "", "unsupported command $command")

                val process = ProcessBuilder(listOf("git") + args)
                    .directory(java.io.File(rootPath))
                    .redirectErrorStream(false)
                    .start()

                val stdout = process.inputStream.bufferedReader(StandardCharsets.UTF_8).use(BufferedReader::readText)
                val stderr = process.errorStream.bufferedReader(StandardCharsets.UTF_8).use(BufferedReader::readText)
                val exitCode = process.waitFor()
                GitCliResult(exitCode, stdout.trim(), stderr.trim())
            }
        }
    }
}
