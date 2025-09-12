using System;

namespace MqttSubscriber.Utilities
{
    public static class Logger
    {
        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{timestamp}] INFO: {message}");
        }

        public static void LogError(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{timestamp}] ERROR: {message}");
            Console.ResetColor();
        }

        public static void LogSuccess(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[{timestamp}] SUCCESS: {message}");
            Console.ResetColor();
        }

        public static void LogWarning(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{timestamp}] WARN: {message}");
            Console.ResetColor();
        }
    }
}