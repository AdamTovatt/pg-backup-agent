using PgBackupAgent.Configuration.FileRetention;

namespace PgBackupAgentTests.Configuration.FileRetention
{
    /// <summary>
    /// Simulator for testing retention policies with different time scenarios.
    /// </summary>
    public class RetentionPolicyTimeSimulator
    {
        private readonly RetentionPolicy _retentionPolicy;
        private readonly List<SimulatedFile> _simulatedFiles;
        private readonly Random _random;
        private DateTime _currentDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetentionPolicyTimeSimulator"/> class.
        /// </summary>
        /// <param name="retentionPolicy">The retention policy to simulate.</param>
        /// <param name="currentDate">The current date for the simulation.</param>
        public RetentionPolicyTimeSimulator(RetentionPolicy retentionPolicy, DateTime currentDate)
        {
            ArgumentNullException.ThrowIfNull(retentionPolicy);

            _retentionPolicy = retentionPolicy;
            _currentDate = currentDate;
            _simulatedFiles = new List<SimulatedFile>();
            _random = new Random();
        }

        /// <summary>
        /// Generates a new simulated file with the current date but a random time.
        /// </summary>
        /// <returns>A new simulated file with a random time on the current date.</returns>
        public SimulatedFile GenerateFile()
        {
            int randomHour = _random.Next(0, 24);
            int randomMinute = _random.Next(0, 60);
            int randomSecond = _random.Next(0, 60);

            DateTime randomTime = _currentDate.Date.AddHours(randomHour).AddMinutes(randomMinute).AddSeconds(randomSecond);

            return new SimulatedFile(randomTime);
        }

        /// <summary>
        /// Simulates moving forward one day by generating two files and advancing the current date.
        /// </summary>
        public void SimulateOneDayForward()
        {
            SimulatedFile file1 = GenerateFile();
            SimulatedFile file2 = GenerateFile();

            _simulatedFiles.Add(file1);
            _simulatedFiles.Add(file2);

            ApplyPolicy();
            _currentDate = _currentDate.AddDays(1);
        }

        /// <summary>
        /// Simulates moving forward by the specified number of days.
        /// </summary>
        /// <param name="numberOfDays">The number of days to simulate forward.</param>
        public void SimulateForward(int numberOfDays)
        {
            for (int i = 0; i < numberOfDays; i++)
            {
                SimulateOneDayForward();
            }
        }

        /// <summary>
        /// Applies the retention policy to all simulated files, removing those that should not be kept.
        /// </summary>
        public void ApplyPolicy()
        {
            for (int i = _simulatedFiles.Count - 1; i >= 0; i--)
            {
                SimulatedFile file = _simulatedFiles[i];
                bool shouldKeep = _retentionPolicy.ShouldKeepFile(file.Date, _currentDate);

                if (!shouldKeep)
                {
                    _simulatedFiles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Gets the count of existing files between two reference dates.
        /// </summary>
        /// <param name="startDate">The start date for the count range.</param>
        /// <param name="endDate">The end date for the count range.</param>
        /// <returns>The number of files that exist between the specified dates.</returns>
        public int GetFileCountBetweenDates(DateTime startDate, DateTime endDate)
        {
            int count = 0;

            foreach (SimulatedFile file in _simulatedFiles)
            {
                if (file.Date >= startDate && file.Date <= endDate)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Represents a simulated file with a specific date.
        /// </summary>
        public readonly struct SimulatedFile
        {
            /// <summary>
            /// Gets the date of the simulated file.
            /// </summary>
            public DateTime Date { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="SimulatedFile"/> struct.
            /// </summary>
            /// <param name="date">The date of the simulated file.</param>
            public SimulatedFile(DateTime date)
            {
                Date = date;
            }
        }
    }
}