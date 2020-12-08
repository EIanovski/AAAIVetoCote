using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AAAI_veto_core
{
	class Program
	{
		public static Random RandomGenerator = new Random();

		static void Main(string[] args)
		{
			// Values of n,m to check
			var agentNumbers = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			var candidateNumbers = new int[] { 2, 3, 4, 5 };

			Simulations.AverageNumberOfWinners(
				agentNumbers,
				candidateNumbers,
				100, // Number of repetitions.
				prof => VotingFunctions.FindVetoCore(prof), // FindVetoByConsumptionWinners for veto by consumption.
				(agents, candidates) => Profile.GenerateICProfile(agents, candidates));

			Console.ReadLine();
		}
	}
}
