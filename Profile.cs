using System.Collections.Generic;
using System.Linq;

namespace AAAI_veto_core
{
	internal class Profile
	{
		public int NumberOfVoters;
		public int NumberOfCandidates;
		private int[,] _profile;

		public IEnumerable<int> Voters
		{
			get
			{
				return Enumerable.Range(0, NumberOfVoters);
			}
		}

		public IEnumerable<int> Candidates
		{
			get
			{
				return Enumerable.Range(0, NumberOfCandidates);
			}
		}

		public Profile(int[,] preferenceMatrix)
		{
			NumberOfVoters = preferenceMatrix.GetLength(0);
			NumberOfCandidates = preferenceMatrix.GetLength(1);
			_profile = preferenceMatrix;
		}

		public static Profile GenerateICProfile(int numberOfAgents, int numberOfCandidates)
		{
			var profile = new int[numberOfAgents, numberOfCandidates];

			for (int i = 0; i < numberOfAgents; i++)
			{
				for (int j = 0; j < numberOfCandidates; j++)
				{
					profile[i, j] = j;
				}
				Utilities.ShuffleRow(profile, i);
			}
			return new Profile(profile);
		}

		public int AgentsIthChoice(int agent, int i)
		{
			return _profile[agent, i];
		}
	}
}