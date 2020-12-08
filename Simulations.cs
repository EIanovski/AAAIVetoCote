using System;
using System.Linq;
using System.Collections.Generic;

namespace AAAI_veto_core
{
	internal class Simulations
	{
		public static void AverageNumberOfWinners(
			IEnumerable<int> agentNumbers,
			IEnumerable<int> candidateNumbers,
			int repetitions,
			Func<Profile, IEnumerable<int>> votingRule,
			Func<int, int, Profile> generateProfile)
		{
			int numberOfAgentValues = agentNumbers.Count();
			int numberOfCandidateValues = candidateNumbers.Count();
			var numberOfWinners = new double[numberOfCandidateValues, numberOfAgentValues];
			var proportionOfWinners = new double[numberOfCandidateValues, numberOfAgentValues];
			for (int agentIndex = 0; agentIndex < numberOfAgentValues; agentIndex++)
			{
				int agents = agentNumbers.ElementAt(agentIndex);
				for (int candidateIndex = 0; candidateIndex < numberOfCandidateValues; candidateIndex++)
				{
					int candidates = candidateNumbers.ElementAt(candidateIndex);
					double aggregateNumberOfWinners = 0;

					for (int repetition = 0; repetition < repetitions; repetition++)
					{
						Profile profile = generateProfile(agents, candidates);
						aggregateNumberOfWinners += votingRule(profile).Count();
					}

					numberOfWinners[candidateIndex, agentIndex] = aggregateNumberOfWinners / repetitions;
					proportionOfWinners[candidateIndex, agentIndex] = (aggregateNumberOfWinners / repetitions) / candidates;
				}
			}
			Console.WriteLine("Number of winners:");

			Console.WriteLine(
				Utilities.FormatLatexTable(
					candidateNumbers.Select(x => x.ToString()).ToArray(),
					agentNumbers.Select(x => x.ToString()).ToArray(),
					numberOfWinners,
					x => String.Format("{0:0.00}", x)));

			Console.WriteLine("Proportion of winners:");

			Console.WriteLine(
				Utilities.FormatLatexTable(
					candidateNumbers.Select(x => x.ToString()).ToArray(),
					agentNumbers.Select(x => x.ToString()).ToArray(),
					proportionOfWinners,
					x => String.Format("{0:0.00}", x)));
		}
	}
}