using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AAAI_veto_core
{
	internal class Utilities
	{
		internal static string FormatLatexTable<T>(
			string[] Xlabels,
			string[] Ylabels,
			T[,] data,
			Func<T, string> format = null)
		{
			if (format == null)
			{
				format = x => x.ToString();
			}
			var output = new StringBuilder();
			output.Append(@"\begin{tabular}{");
			output.Append("c | ");
			for (int i = 0; i < Xlabels.Length; i++)
			{
				output.Append("c ");
			}
			output.Append(@"}");
			output.Append(Environment.NewLine);

			output.Append("& ");
			for (int i = 0; i < Xlabels.Length; i++)
			{
				output.Append(Xlabels[i]);
				if (i < Xlabels.Length - 1)
				{
					output.Append(@" & ");
				}
			}
			output.Append(@"\\");
			output.Append(Environment.NewLine);
			output.Append(@"\hline");
			output.Append(Environment.NewLine);
			for (int i = 0; i < Ylabels.Length; i++)
			{
				output.Append(Ylabels[i]);
				output.Append(@" & ");
				for (int j = 0; j < Xlabels.Length; j++)
				{
					output.Append(format(data[j, i]));
					if (j < Xlabels.Length - 1)
					{
						output.Append(@" & ");
					}
				}
				output.Append(@"\\");
				output.Append(Environment.NewLine);
			}
			output.Append(@"\end{tabular}");
			return output.ToString();
		}

		internal static void ShuffleRow(int[,] array, int row)
		{

			int n = array.GetLength(1);
			while (n > 1)
			{
				n--;
				int i = Program.RandomGenerator.Next(n + 1);
				int temp = array[row, i];
				array[row, i] = array[row, n];
				array[row, n] = temp;
			}
		}

		public static Pair<int, int> GetMoulinCoefficients(int n, int m)
		{
			int[] BezoutCoefficients = ExtendedEuclidean(n, m);
			int gcd = BezoutCoefficients[0];
			int r = BezoutCoefficients[1] * -1;
			int t = BezoutCoefficients[2];

			while (t <= gcd * n || r <= 0 || t <= 0)
			{
				r += m;
				t += n;
			}
			return new Pair<int, int>(r, t);
		}

		public static int[] ExtendedEuclidean(int firstNumber, int secondNumber)
		{
			int[] gcdAndCoeffs = { 0, 0, 0 };
			int[] firstCoefficient = { 1, 0 };
			int[] secondCoefficient = { 0, 1 };
			int quotient = 0;

			while (true)
			{
				quotient = firstNumber / secondNumber;
				firstNumber = firstNumber % secondNumber;
				firstCoefficient[0] = firstCoefficient[0] - quotient * firstCoefficient[1];
				secondCoefficient[0] = secondCoefficient[0] - quotient * secondCoefficient[1];
				if (firstNumber == 0)
				{
					gcdAndCoeffs[0] = secondNumber; gcdAndCoeffs[1] = firstCoefficient[1]; gcdAndCoeffs[2] = secondCoefficient[1];
					return gcdAndCoeffs;
				};
				quotient = secondNumber / firstNumber;
				secondNumber = secondNumber % firstNumber;
				firstCoefficient[1] = firstCoefficient[1] - quotient * firstCoefficient[0];
				secondCoefficient[1] = secondCoefficient[1] - quotient * secondCoefficient[0];
				if (secondNumber == 0)
				{
					gcdAndCoeffs[0] = firstNumber; gcdAndCoeffs[1] = firstCoefficient[0]; gcdAndCoeffs[2] = secondCoefficient[0];
					return gcdAndCoeffs;
				};
			}
		}

		internal static int LargestBicliqueViaMaxFlow(Profile profile, int c, int r, int t)
		{
			Dictionary<int, Dictionary<int, Dictionary<MPM.FlowGraphKeys, int>>> flowGraph =
				BuildFlowGraph(profile, c, r, t);
			int largestMatching = MPM.FindMaxFlow(0, 1, flowGraph);
			return (profile.NumberOfVoters * r + (profile.NumberOfCandidates - 1) * t) - largestMatching;
		}

		private static Dictionary<int, Dictionary<int, Dictionary<MPM.FlowGraphKeys, int>>> BuildFlowGraph(Profile profile, int c, int r, int t)
		{
			var flowGraph = new Dictionary<int, Dictionary<int, Dictionary<MPM.FlowGraphKeys, int>>>();

			var nodes = Enumerable.Range(0, profile.NumberOfCandidates + profile.NumberOfVoters + 2);
			foreach (int node in nodes)
			{
				flowGraph[node] = new Dictionary<int, Dictionary<MPM.FlowGraphKeys, int>>();
			}
			const int SOURCE_INDEX = 0;
			const int SINK_INDEX = 1;
			foreach (int agent in profile.Voters)
			{
				int agentNodeIndex = agent + 2;
				flowGraph[SOURCE_INDEX][agentNodeIndex] = new Dictionary<MPM.FlowGraphKeys, int>();
				flowGraph[SOURCE_INDEX][agentNodeIndex][MPM.FlowGraphKeys.Cap] = r;
				flowGraph[SOURCE_INDEX][agentNodeIndex][MPM.FlowGraphKeys.Flow] = 0;
				int worstCandidateIndex = profile.NumberOfCandidates - 1;
				for (int candidateIndex = worstCandidateIndex; candidateIndex >= 0; candidateIndex--)
				{
					int candidate = profile.AgentsIthChoice(agent, candidateIndex);
					if (candidate == c)
					{
						break;
					}
					int candidateNodeIndex = 2 + profile.NumberOfVoters + candidate;
					flowGraph[agentNodeIndex][candidateNodeIndex] = new Dictionary<MPM.FlowGraphKeys, int>();
					flowGraph[agentNodeIndex][candidateNodeIndex][MPM.FlowGraphKeys.Cap] = r;
					flowGraph[agentNodeIndex][candidateNodeIndex][MPM.FlowGraphKeys.Flow] = 0;
					if (!flowGraph[candidateNodeIndex].ContainsKey(SINK_INDEX))
					{
						flowGraph[candidateNodeIndex][SINK_INDEX] = new Dictionary<MPM.FlowGraphKeys, int>();
						flowGraph[candidateNodeIndex][SINK_INDEX][MPM.FlowGraphKeys.Cap] = t;
						flowGraph[candidateNodeIndex][SINK_INDEX][MPM.FlowGraphKeys.Flow] = 0;
					}
				}
			}
			return flowGraph;
		}


	}
}