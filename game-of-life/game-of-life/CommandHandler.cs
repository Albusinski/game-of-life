using System;
using System.Collections.Generic;
using System.Text;

namespace game_of_life
{
    public sealed class CommandHandler // sealed = holy opsec
    {
        public event System.Action<int, int>? GridRequested;

        public void Execute(string input)
        {
            string[] parts = input.ToLower().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return;

            switch (parts[0].ToLowerInvariant()) //ToLower nie działa :p
            {
                case "grid":
                    CreateGrid(parts);
                    break;

                case "start":
                    Start(parts);
                    break;

                case "stop":
                    Stop();
                    break;
            }
        }

        private void CreateGrid(string[] args)
        {
            if (args.Length != 3)
            {
                return;
            }

            if (!int.TryParse(args[1], out int width) || width <= 0)
            {
                return;
            }

            if (!int.TryParse(args[2], out int height) || height <= 0)
            {
                return;
            }

            GridRequested?.Invoke(width, height);
        }

        private void Start(string[] args)
        {
            if (args.Length > 3)
            {
                return;
            }

            int interval = 100;
            int? steps = null;

            if(args.Length >= 2 && (!int.TryParse(args[1], out interval) || interval <= 0))
            {
                return;
            }

            if (args.Length == 3)
            {
                if (!int.TryParse(args[2], out int parsedSteps) || parsedSteps <= 0)
                {
                    return;
                }

                steps =parsedSteps;
            }
            //interval: odstęp między generacjami w ms
            //steps == null: symulacja bez końca.
            //steps == x: x generacji w symulacji
        }

        private void Stop()
        {
            //pauza symulacji
        }
    }
}
