/*
Adapted from the python code of Giorgos Kominos. Original copyright license follows:

 === LICENCE ====
Copyright (c) <2013>, <Giorgos Komninos>
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace AAAI_veto_core
{
	internal class MPM
	{

		public static int FindMaxFlow(
			int source,
			int sink,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> flowGraph)
		{
			while (true)
			{
				Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> levelGraph = BuildLevelGraph(source, sink, flowGraph);
				if (levelGraph == null)
				{
					break;
				}
				Dictionary<int, Dictionary<int, int>> flow = ConstructBlockingFlow(source, sink, levelGraph, flowGraph);
				AddFlow(flowGraph, flow);
			}
			return flowGraph[source].Keys.Sum(x => flowGraph[source][x][FlowGraphKeys.Flow]);
		}

		private static void AddFlow(
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> flowGraph, 
			Dictionary<int, Dictionary<int, int>> flow)
		{
			foreach (int source in flow.Keys)
			{
				foreach (int node in flow[source].Keys)
				{
					flowGraph[source][node][FlowGraphKeys.Flow] += flow[source][node];
				}
			}
		}

		

		private static Dictionary<int, Dictionary<int, int>> ConstructBlockingFlow(
			int source,
			int sink,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> auxiliaryGraph,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> flowGraph)
		{
			var blockingFlow = new Dictionary<int, Dictionary<int, int>>();
			while (true)
			{
				Dictionary<int, Pair<int, int>> throughput = CalculateThroughput(source, sink, auxiliaryGraph);
				bool workToDo = DeleteZeroThroughput(source, sink, auxiliaryGraph, throughput);
				if (!workToDo)
				{
					return blockingFlow;
				}
				if (!auxiliaryGraph.ContainsKey(source) || !auxiliaryGraph.ContainsKey(sink))
				{
					return blockingFlow;
				}
				Pair<int, int> minThroughput = new Pair<int, int>(-1, int.MaxValue);
				foreach (int node in throughput.Keys)
				{
					int currentThroughput = Math.Min(throughput[node].First, throughput[node].Second);
					if (currentThroughput < minThroughput.Second)
					{
						minThroughput = new Pair<int, int>(node, currentThroughput);
					}
				}
				Push(sink, minThroughput.First, minThroughput.Second, auxiliaryGraph, throughput, blockingFlow);
				Pull(source, minThroughput.First, minThroughput.Second, auxiliaryGraph, throughput, blockingFlow);
			}
		}

		private static bool DeleteZeroThroughput(
			int source, 
			int sink, 
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> auxiliaryGraph, 
			Dictionary<int, Pair<int, int>> throughput)
		{
			while (true)
			{
				bool hazSero = false;
				var throughKeys = new List<int>(throughput.Keys);
				foreach (int node in throughKeys)
				{
					int inCap = throughput[node].First;
					int outCap = throughput[node].Second;
					int minValue = Math.Min(inCap, outCap);
					if (minValue == 0)
					{
						if (node == source || node == sink)
						{
							return false;
						}
						hazSero = true;

						foreach (int nodeToUpdate in auxiliaryGraph[node].Keys)
						{
							throughput[nodeToUpdate].First -= auxiliaryGraph[node][nodeToUpdate][FlowGraphKeys.Cap];
						}

						foreach (int nodeToUpdate in auxiliaryGraph.Keys)
						{
							if (auxiliaryGraph[nodeToUpdate].ContainsKey(node))
							{
								throughput[nodeToUpdate].Second -= auxiliaryGraph[nodeToUpdate][node][FlowGraphKeys.Cap];
							}
						}
						DeleteNode(auxiliaryGraph, node);
						throughput.Remove(node);
					}
				}
				if (!hazSero)
				{
					break;
				}
			}
			return true;
		}

		private static void Pull(
			int source,
			int referenceNode,
			int referenceThroughput,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> auxiliaryGraph,
			Dictionary<int, Pair<int, int>> throughput,
			Dictionary<int, Dictionary<int, int>> blockingFlow)
		{
			var queue = new Queue<int>();
			queue.Enqueue(referenceNode);
			var howMuchToPull = new Dictionary<int, int>();
			foreach (int node in auxiliaryGraph.Keys)
			{
				howMuchToPull[node] = 0;
			}
			howMuchToPull[referenceNode] = referenceThroughput;
			while (queue.Count > 0)
			{
				int pullFrom = queue.Dequeue();
				if (howMuchToPull[pullFrom] == 0)
				{
					continue;
				}
				foreach (int pullTo in auxiliaryGraph.Keys)
				{
					Dictionary<int, Dictionary<FlowGraphKeys, int>> outEdges = auxiliaryGraph[pullTo];
					if (outEdges.ContainsKey(pullFrom))
					{
						if (outEdges[pullFrom].ContainsKey(FlowGraphKeys.Used))
						{
							continue;
						}
						int pullAmount = Math.Min(outEdges[pullFrom][FlowGraphKeys.Cap], howMuchToPull[pullFrom]);
						outEdges[pullFrom][FlowGraphKeys.Cap] -= pullAmount;
						if (outEdges[pullFrom][FlowGraphKeys.Cap] == 0)
						{
							outEdges[pullFrom][FlowGraphKeys.Used] = 1;
							throughput[pullFrom].First -= pullAmount;
							throughput[pullTo].Second += pullAmount;
						}
						howMuchToPull[pullFrom] -= pullAmount;
						howMuchToPull[pullTo] += pullAmount;
						queue.Enqueue(pullTo);
						int direction = outEdges[pullFrom][FlowGraphKeys.Direction];
						int start, end;
						if (direction == -1)
						{
							start = pullFrom;
							end = pullTo;
							pullAmount = (-1) * pullAmount;
						}
						else
						{
							start = pullTo;
							end = pullFrom;
						}
						if (!blockingFlow.ContainsKey(start))
						{
							blockingFlow[start] = new Dictionary<int, int>();
						}
						if (!blockingFlow[start].ContainsKey(end))
						{
							blockingFlow[start][end] = 0;
						}
						blockingFlow[start][end] += pullAmount;
					}
				}
			}

		}

		private static void Push(
			int sink, 
			int node, 
			int throughputValue, 
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> auxiliaryGraph, 
			Dictionary<int, Pair<int, int>> throughput, 
			Dictionary<int, Dictionary<int, int>> blockingFlow)
		{
			var queue = new Queue<int>();
			queue.Enqueue(node);
			var howMuchToPush = new Dictionary<int, int>();
			foreach (int i in auxiliaryGraph.Keys)
			{
				howMuchToPush[i] = 0;
			}
			howMuchToPush[node] = throughputValue;
			List<int> flows = new List<int>();
			while (queue.Count > 0)
			{
				int currentNode = queue.Dequeue();
				if (howMuchToPush[currentNode] == 0 || currentNode == sink)
				{
					continue;
				}
				foreach (int neighbour in auxiliaryGraph[currentNode].Keys)
				{
					if (auxiliaryGraph[currentNode][neighbour].ContainsKey(FlowGraphKeys.Used))
					{
						continue;
					}
					int pushAmount = Math.Min(auxiliaryGraph[currentNode][neighbour][FlowGraphKeys.Cap], howMuchToPush[currentNode]);
					auxiliaryGraph[currentNode][neighbour][FlowGraphKeys.Cap] -= pushAmount;
					if (auxiliaryGraph[currentNode][neighbour][FlowGraphKeys.Cap] == 0)
					{
						auxiliaryGraph[currentNode][neighbour][FlowGraphKeys.Used] = 1;
						foreach (int otherNeighbour in auxiliaryGraph[currentNode].Keys)
						{
							throughput[otherNeighbour].First -= pushAmount;
						}
					}
					howMuchToPush[currentNode] -= pushAmount;
					howMuchToPush[neighbour] += pushAmount;
					queue.Enqueue(neighbour);
					int direction = auxiliaryGraph[currentNode][neighbour][FlowGraphKeys.Direction];
					int start, end;
					if (direction == -1)
					{
						start = neighbour;
						end = currentNode;
						pushAmount = (-1) * pushAmount;
					}
					else
					{
						start = currentNode;
						end = neighbour;
					}
					if (!blockingFlow.ContainsKey(start))
					{
						blockingFlow[start] = new Dictionary<int, int>();
					}
					if (!blockingFlow[start].ContainsKey(end))
					{
						blockingFlow[start][end] = 0;
					}
					blockingFlow[start][end] += pushAmount;
				}
			}
		}



		private static Dictionary<int, Pair<int, int>> CalculateThroughput(
			int source,
			int sink,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> auxiliaryGraph)
		{
			var throughput = new Dictionary<int, Pair<int, int>>();
			foreach (int node in auxiliaryGraph.Keys)
			{
				throughput[node] = new Pair<int, int>(0, 0);
			}
			foreach (int node in auxiliaryGraph.Keys)
			{
				if (node == source)
				{
					throughput[node].First = int.MaxValue;
				}
				foreach (int neighbour in auxiliaryGraph[node].Keys)
				{
					throughput[neighbour].First += auxiliaryGraph[node][neighbour][FlowGraphKeys.Cap];
				}
				int outCap;
				if (node == sink)
				{
					outCap = int.MaxValue;
				}
				else
				{
					outCap = auxiliaryGraph[node].Keys.Sum(x => auxiliaryGraph[node][x][FlowGraphKeys.Cap]);
				}
				throughput[node].Second = outCap;
			}
			return throughput;
		}


		private static Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> BuildLevelGraph(
			int source,
			int sink,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> flowGraph)
		{
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> residualGraph = BuildResidualGraph(source, sink, flowGraph);
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> auxiliaryGraph = BuildAuxiliaryGraph(source, sink, residualGraph);
			return auxiliaryGraph;
		}

		private static Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> BuildAuxiliaryGraph(
			int source,
			int sink,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> residualGraph)
		{
			var auxiliaryGraph = new Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>>();
			var queue = new Queue<int>();
			queue.Enqueue(source);
			var levels = new Dictionary<int, int>();
			levels[source] = 0;
			HashSet<int> visited = new HashSet<int>();
			visited.Add(source);
			while (queue.Count > 0)
			{
				int currentNode = queue.Dequeue();
				auxiliaryGraph[currentNode] = new Dictionary<int, Dictionary<FlowGraphKeys, int>>();
				foreach (int neighbour in residualGraph[currentNode].Keys)
				{
					if (levels.ContainsKey(neighbour) && neighbour != sink)
					{
						continue;
					}
					auxiliaryGraph[currentNode][neighbour] = residualGraph[currentNode][neighbour];
					levels[neighbour] = levels[currentNode] + 1;
					if (!visited.Contains(neighbour))
					{
						queue.Enqueue(neighbour);
					}
					visited.Add(neighbour);
				}
			}
			if (!auxiliaryGraph.ContainsKey(sink))
			{
				return null;
			}
			int sinkLevel = levels[sink];
			bool complete = false;
			foreach (int node in levels.Keys)
			{
				if (levels[node] < sinkLevel)
				{
					continue;
				}
				if (node == sink)
				{
					complete = true;
					continue;
				}
				DeleteNode(auxiliaryGraph, node);
			}
			if (complete)
			{
				return auxiliaryGraph;
			}
			else
			{
				return null;
			}
		}

		private static void DeleteNode(
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> flowGraph, 
			int deletee)
		{
			foreach (int node in flowGraph.Keys)
			{
				if (flowGraph[node].ContainsKey(deletee))
				{
					flowGraph[node].Remove(deletee);
				}
			}
			if (flowGraph.ContainsKey(deletee))
			{
				flowGraph.Remove(deletee);
			}
		}


		private static Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> BuildResidualGraph(
			int source,
			int sink,
			Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>> flowGraph)
		{
			var residualGraph = new Dictionary<int, Dictionary<int, Dictionary<FlowGraphKeys, int>>>();
			var queue = new Queue<int>();
			queue.Enqueue(source);
			var visited = new HashSet<int>();
			visited.Add(source);
			while (queue.Count > 0)
			{
				int currentNode = queue.Dequeue();
				foreach (int neighbour in flowGraph[currentNode].Keys)
				{
					int potential =
						flowGraph[currentNode][neighbour][FlowGraphKeys.Cap] -
						flowGraph[currentNode][neighbour][FlowGraphKeys.Flow];
					if (!residualGraph.ContainsKey(currentNode))
					{
						residualGraph[currentNode] = new Dictionary<int, Dictionary<FlowGraphKeys, int>>();
					}
					if (!residualGraph.ContainsKey(neighbour))
					{
						residualGraph[neighbour] = new Dictionary<int, Dictionary<FlowGraphKeys, int>>();
					}
					if (potential > 0)
					{
						residualGraph[currentNode][neighbour] = new Dictionary<FlowGraphKeys, int>();
						residualGraph[currentNode][neighbour][FlowGraphKeys.Cap] = potential;
						residualGraph[currentNode][neighbour][FlowGraphKeys.Direction] = 1;
					}
					int flow = flowGraph[currentNode][neighbour][FlowGraphKeys.Flow];
					if (flow > 0)
					{
						residualGraph[neighbour][currentNode] = new Dictionary<FlowGraphKeys, int>();
						residualGraph[neighbour][currentNode][FlowGraphKeys.Cap] = flow;
						residualGraph[neighbour][currentNode][FlowGraphKeys.Flow] = 0;
						residualGraph[neighbour][currentNode][FlowGraphKeys.Direction] = -1;
					}
					if (!visited.Contains(neighbour))
					{
						queue.Enqueue(neighbour);
					}
					visited.Add(neighbour);
				}
			}
			return residualGraph;
		}


		public enum FlowGraphKeys
		{
			Cap,
			Flow,
			Direction,
			Used
		}
	}
}