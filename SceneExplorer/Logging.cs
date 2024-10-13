using System;
using System.Diagnostics;
using Colossal.Logging;

namespace SceneExplorer
{
    public static class Logging
    {
        private static ILog _logger;

        static Logging() {
            try
            {
                _logger = LogManager.GetLogger("SceneExplorer").SetShowsErrorsInUI(false);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        public static void Info(string message) {
            _logger.Info(message);
        }
        
        [Conditional("DEBUG")]
        public static void Debug(string message) {
            _logger.Info(message);
        }
        
        [Conditional("DEBUG_EVALUATION")]
        public static void DebugEvaluation(string message) {
            _logger.Info(message);
        }
        
        [Conditional("DEBUG_UI")]
        public static void DebugUI(string message) {
            _logger.Info(message);
        }

        public static void Warning(string message) {
            _logger.Warn(message);
        }

        public static void Error(string message) {
            _logger.Error(message);
        }
    }
}
