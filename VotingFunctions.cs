using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace AAAI_veto_core
{
	internal class VotingFunctions
	{
		public static TiedWinners FindVetoByConsumptionWinners(Profile profile)
		{
			return new TiedWinners(FindConsumptionLottery(profile).Keys(), "Veto by consumption winners");
		}

		public static Lottery<double> FindConsumptionLottery(Profile profile)
		{
			var remainingCapacity = new Dictionary<int, double>();
			var tracker = new WorstCandidateTracker(profile);
			var eatenBy = new Dictionary<int, HashSet<int>>();
			var eatenNext = new HashSet<int>();
			foreach (int tastyCandidate in profile.Candidates)
			{
				remainingCapacity[tastyCandidate] = 1;
			}

			while (tracker.RemainingCandidates.Count > 0)
			{
				foreach (int tastyCandidate in eatenNext)
				{
					eatenBy.Remove(tastyCandidate);
				}
				foreach (int hungryAgent in profile.Voters)
				{
					int tastyCandidate = tracker.IdOfWorstCandidate(hungryAgent);
					if (!eatenBy.ContainsKey(tastyCandidate))
					{
						eatenBy[tastyCandidate] = new HashSet<int>();
					}
					eatenBy[tastyCandidate].Add(hungryAgent);
				}

				double minTime = double.PositiveInfinity;
				eatenNext = new HashSet<int>();

				foreach (int tastyCandidate in eatenBy.Keys)
				{
					int eatingSpeed = eatenBy[tastyCandidate].Count;
					double newTime = remainingCapacity[tastyCandidate] / eatingSpeed;
					if (newTime < minTime)
					{
						minTime = newTime;
						eatenNext = new HashSet<int>();
					}
					if (newTime == minTime)
					{
						eatenNext.Add(tastyCandidate);
					}
				}
				foreach (int tastyCandidate in eatenBy.Keys)
				{
					int eatingSpeed = eatenBy[tastyCandidate].Count;
					remainingCapacity[tastyCandidate] -= minTime * eatingSpeed;
				}
				tracker.RemainingCandidates.ExceptWith(eatenNext);
			}

			var lottery = new Dictionary<int, double>();
			foreach (int tastyCandidate in eatenNext)
			{
				int eatingSpeed = eatenBy[tastyCandidate].Count;
				lottery[tastyCandidate] = 1.0 * eatingSpeed / profile.NumberOfVoters;
			}
			return new Lottery<double>(lottery, "Veto by consumption lottery");
		}

		public static TiedWinners FindVetoCore(Profile profile, CoreAlgorithm algo = CoreAlgorithm.MaxFlow)
		{
			Pair<int, int> coefficients =
				Utilities.GetMoulinCoefficients(profile.NumberOfVoters, profile.NumberOfCandidates);
			int blockingBicliqueSize = profile.NumberOfCandidates * coefficients.Second;

			var core = new HashSet<int>();

			foreach (int c in profile.Candidates)
			{

				int maxBicliqueSize;
				switch (algo)
				{
					case CoreAlgorithm.MaxFlow:
						{
							maxBicliqueSize = Utilities.LargestBicliqueViaMaxFlow(
								profile,
								c,
								coefficients.First,
								coefficients.Second);
							break;
						}
					default:
						{
							throw new NotImplementedException("No implementation for core algorithm.");
						}
				};
				if (maxBicliqueSize < blockingBicliqueSize)
				{
					core.Add(c);
				}
			}

			return new TiedWinners(core, "Veto core");
		}

		public enum CoreAlgorithm
		{
			LinearProgramming,
			Konig,
			MaxFlow
		}
	}

	public interface ElectionResult
	{
		void Print();

		string GetName();
	}

	public class Lottery<T> : ElectionResult, IEnumerable<KeyValuePair<int, T>>
	{
		Dictionary<int, T> _lottery;
		string _name;

		public T this[int index]
		{
			get
			{
				return _lottery[index];
			}

			set
			{
				_lottery[index] = value;
			}
		}

		public Lottery(Dictionary<int, T> lottery, string name)
		{
			_lottery = lottery;
			_name = name;
		}

		public IEnumerable<int> Keys()
		{
			return _lottery.Keys;
		}

		public bool ContainsKey(int key)
		{
			return _lottery.ContainsKey(key);
		}

		public void Print()
		{
			foreach (int key in _lottery.Keys)
			{
				Console.WriteLine(key + ": " + _lottery[key]);
			}
		}

		public string GetName()
		{
			return _name;
		}

		public IEnumerator<KeyValuePair<int, T>> GetEnumerator()
		{
			return _lottery.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	public class TiedWinners : ElectionResult, IEnumerable<int>
	{
		IEnumerable<int> _winners;
		string _name;

		public TiedWinners(IEnumerable<int> winners, string name)
		{
			_winners = winners;
			_name = name;
		}

		public void Print()
		{
			StringBuilder output = new StringBuilder();
			foreach (int winner in _winners)
			{
				output.Append(winner + ", ");
			}
			output.Remove(output.Length - 2, 2);
			Console.WriteLine(output.ToString());
		}

		public string GetName()
		{
			return _name;
		}

		public IEnumerator<int> GetEnumerator()
		{
			return _winners.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}