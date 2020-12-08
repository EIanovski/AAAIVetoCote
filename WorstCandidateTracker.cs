using System.Collections.Generic;

namespace AAAI_veto_core
{
	public class WorstCandidateTracker
	{
		Profile __profile;
		public HashSet<int> RemainingCandidates;
		Dictionary<int, int> __indexOfWorstRemainingCandidate;

		internal WorstCandidateTracker(Profile p)
		{
			__profile = p;
			RemainingCandidates = new HashSet<int>(__profile.Candidates);

			__indexOfWorstRemainingCandidate = new Dictionary<int, int>();
			int indexOfWorstCandidate = __profile.NumberOfCandidates - 1;
			for (int i = 0; i < __profile.NumberOfVoters; i++)
			{
				__indexOfWorstRemainingCandidate[i] = indexOfWorstCandidate;
			}
		}

		public int IndexOfWorstCandidate(int agent)
		{
			while (!RemainingCandidates.Contains(__profile.AgentsIthChoice(agent, __indexOfWorstRemainingCandidate[agent])))
			{
				__indexOfWorstRemainingCandidate[agent]--;
			}
			return __indexOfWorstRemainingCandidate[agent];
		}

		public int IdOfWorstCandidate(int agent)
		{
			return __profile.AgentsIthChoice(agent, IndexOfWorstCandidate(agent));
		}

		public void RemoveWorstCandidate(int agent)
		{
			RemainingCandidates.Remove(IdOfWorstCandidate(agent));
		}
	}

}