
namespace game_of_life
{
    public class CommandHandler
    {
        public void Execute(string input)
        {
            string[] parts = input.ToLower().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return;

            switch (parts[0])
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
            //tworzenie grid
        }

        private void Start(string[] args)
        {
            int interval = 100;
            int? steps = null;

            if (args.Length > 1 &&
                (!int.TryParse(args[1], out interval) || interval <= 0))
                return;

            if (args.Length > 2 &&
                (!int.TryParse(args[2], out int parsedSteps) || parsedSteps <= 0))
                return;

            if (args.Length > 2)
                steps = int.Parse(args[2]);

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
