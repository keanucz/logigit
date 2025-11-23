package com.github.keanucz.logigit.ipc

import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.components.Service

@Service
class LogiGitIpcApp {
    init {
        ApplicationManager.getApplication().invokeLater {
            LogiGitIpcService.instance().start()
        }
    }
}

