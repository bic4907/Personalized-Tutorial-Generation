using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgentsExamples;
using Unity.MLAgents.Integrations.Match3;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.MLAgentsExamples
{

    
    public class GeneticAlgorithm
    {
        private static GeneticAlgorithm _Instance = null;
        private float KnowledgeAlmostRatio = 1.0f;
        public int ChromosomeLength = 0;
        public int PopulationSize = 10;
        private List<Chromosome> Population;

        public Match3Board m_Board = null;
        public SkillKnowledge m_Knowledge = null;
        public int RefNumCellTypes;
        public GeneratorReward RewardMode = GeneratorReward.Score;


        private SelectMethod m_SelectMethod = SelectMethod.RouletteWheel;


        public static GeneticAlgorithm Instance { get {
            if (_Instance == null)
            {
                _Instance = new GeneticAlgorithm();
            }
            return _Instance;
        }}

     
        public float FillEmpty(Match3Board board, SkillKnowledge knowledge, int generation)
        {
            int _emptyCellCount = board.GetEmptyCellCount();
            
            m_Board = board;
            m_Knowledge = knowledge;
            RefNumCellTypes = board.NumCellTypes;
            ChromosomeLength = _emptyCellCount;

            InitializePopulation();

            for (int i = 0; i < generation; i++)
            {
                Evolution(generation);
            }

            Chromosome chromosome = GetBestIndividual();

            foreach (int cellType in chromosome.Genes)
            {
                board.SpawnColoredBlock(cellType);               
            }

            return chromosome.Fitness;
        }

        private void Crossover(List<Chromosome> offspring, double prob = 1)
        {
            // offspring = offspring.Select(x => x.DeepCopy()).ToList();
        
            List<int> idx = Enumerable.Range(0, offspring.Count).ToList();
            List<Chromosome> shuffledOffspring = new List<Chromosome>();
            Shuffle(idx.ToArray());

            foreach (int i in idx)
            {
                shuffledOffspring.Add(offspring[i]);
            }

            offspring = shuffledOffspring;

            int median = offspring.Count / 2;

            for (int offspring_i = 0; offspring_i < median; offspring_i++)
            {
                if (new System.Random().NextDouble() <= prob)
                {
                    int[] individual_1 = offspring[offspring_i].Genes.ToArray();
                    int[] individual_2 = offspring[offspring_i + median].Genes.ToArray();

                    // Get swapping point and swap individual_1 and _2
                    int swap_point = new System.Random().Next(0, ChromosomeLength);
                    for (int gene_index = swap_point; gene_index < ChromosomeLength; gene_index++)
                    {
                        int temp = individual_1[gene_index];
                        individual_1[gene_index] = individual_2[gene_index];
                        individual_2[gene_index] = temp;
                    }

                    offspring[offspring_i].Genes = individual_1.ToList();
                    offspring[offspring_i + median].Genes = individual_2.ToList();
                }
            }

            // return offspring;
        }

        private void Mutation(List<Chromosome> offspring, double prob = 0.01)
        {
            // offspring = offspring.Select(x => x.DeepCopy()).ToList();

            for (int individual_index = 0; individual_index < offspring.Count; individual_index++)
            {
                for (int gene_index = 0; gene_index < ChromosomeLength; gene_index++)
                {
                    if (new System.Random().NextDouble() <= prob)
                    {
                        int rand_int = new System.Random().Next(0, RefNumCellTypes);
                        offspring[individual_index].Genes[gene_index] = rand_int;
                    }
                }
            }
        }

        public bool IsBoardMadeMatch(Match3Board board)
        {
            Match3Board _simulationBoard = board.DeepCopy();
            bool madeMatch = _simulationBoard.MarkMatchedCells();

            _simulationBoard = null;

            return madeMatch;
        }

        public float CalcFitness(Chromosome chromosome)
        {

            Match3Board _simulationBoard = m_Board.DeepCopy();

            foreach (int cellType in chromosome.Genes)
            {
                _simulationBoard.SpawnColoredBlock(cellType);               
            }
            Debug.Assert(_simulationBoard.GetEmptyCellCount() == 0, "The value should not be zero");

            SkillKnowledge playerKnowledge = m_Knowledge.DeepCopy();
            
            if (IsBoardMadeMatch(_simulationBoard))
            {
                _simulationBoard = null;
                return -float.MaxValue;
            }
            Move move = GreedyMatch3Solver.GetAction(_simulationBoard);
            _simulationBoard.MakeMove(move);
            _simulationBoard.MarkMatchedCells();

            _simulationBoard.ClearMatchedCells();
            _simulationBoard.SpawnSpecialCells();
            var createdPieces = _simulationBoard.GetLastCreatedPiece();

            float score = 0.0f;
    

            switch(RewardMode)
            {
                case GeneratorReward.Score:
                    score += createdPieces.Count;
                    break;
                case GeneratorReward.Knowledge:

                    foreach ((int CellType, int SpecialType) piece in createdPieces)
                    {
                        bool isReached = playerKnowledge.IsMatchCountAlmostReachedTarget((PieceType)piece.SpecialType, KnowledgeAlmostRatio);
                        if (!isReached) // Have to learn
                        {
                            score += (float)Math.Pow(MCTS.Instance.PieceScoreWeight[(PieceType)piece.SpecialType], 1);
                            playerKnowledge.IncreaseMatchCount((PieceType)piece.SpecialType);
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            _simulationBoard = null;

            return score;
        
        }

        private float GetFitness(Chromosome chromosome)
        {
            if (chromosome.IsFitnessCalced)
            {
                return chromosome.Fitness;
            }
            else
            {
                chromosome.Fitness = CalcFitness(chromosome);
                chromosome.IsFitnessCalced = true;
                return chromosome.Fitness;
            }
        }

        private List<Chromosome> Selection()
        {
            if (m_SelectMethod == SelectMethod.RouletteWheel)
            {
                var offspring = Population;
                // List<Chromosome> offspring = Population.Select(x => x.DeepCopy()).ToList();
                List<Chromosome> newPopulation = new List<Chromosome>();

                float[] fitnessValues = offspring.Select(GetFitness).ToArray();
                float minFitness = fitnessValues.Min();
                float maxFitness = fitnessValues.Max();
                fitnessValues = fitnessValues.Select(f => (f - minFitness) / (maxFitness - minFitness)).ToArray();

                float sumFitness = fitnessValues.Sum();
                float[] ratios = fitnessValues.Select(f => f / sumFitness).ToArray();

                for (int i = 0; i < PopulationSize; i++)
                {
                    double x = new System.Random().NextDouble();
                    int k = 0;

                    while (k < PopulationSize - 1 && x > (ratios.Take(k + 1).Sum()))
                    {
                        k++;
                    }

                    newPopulation.Add(offspring[k]);
                }

                return newPopulation;
            }
            else if (m_SelectMethod == SelectMethod.Tournament)
            {
                List<Chromosome> newPopulation = new List<Chromosome>();

                for (int candidate1 = 0; candidate1 < PopulationSize; candidate1++)
                {
                    int candidate2 = new System.Random().Next(0, PopulationSize);

                    Chromosome individual_1 = Population[candidate1];
                    Chromosome individual_2 = Population[candidate2];

                    float fitness_1 = GetFitness(individual_1);
                    float fitness_2 = GetFitness(individual_2);

                    if (fitness_1 > fitness_2)
                    {
                        newPopulation.Add(individual_1);
                    }
                    else
                    {
                        newPopulation.Add(individual_2);
                    }
                }

                return newPopulation;
            }
            else
            {
                throw new ArgumentOutOfRangeException(m_SelectMethod.ToString());
            }
        }

        private List<Chromosome> Sorting(List<Chromosome> offspring)
        {
            float[] fitnessValue = Enumerable.Range(0, PopulationSize).Select(i => GetFitness(offspring[i])).ToArray();
            List<float> sortedFitness = fitnessValue.OrderByDescending(f => f).ToList();

            List<Chromosome> sortedOffspring = new List<Chromosome>();

            for (int i = 0; i < offspring.Count; i++)
            {
                for (int j = 0; j < offspring.Count; j++)
                {
                    if (sortedFitness[i] == fitnessValue[j])
                    {
                        sortedOffspring.Add(offspring[j]);
                        break;
                    }
                }
            }

            return sortedOffspring;
        }

        public void Evolution(int generation)
        {

            List<Chromosome> offspring2 = Population.Select(x => x.DeepCopy()).ToList();

            offspring2 = Sorting(offspring2);
            List<Chromosome> offspring = Selection();
            Crossover(offspring, 0.9);
            Mutation(offspring, 0.02);

            offspring = Sorting(offspring);
            for (int k = generation / 5; k < offspring2.Count; k++)
            {
                offspring2[k] = offspring[k - generation / 5].DeepCopy();
            }
            offspring = offspring2.Select(x => x.DeepCopy()).ToList();
            Population = offspring;

        }

        public float GetAverageFitness()
        {
            float[] fitnessValues = Population.Select(GetFitness).ToArray();
            float sumFitness = fitnessValues.Sum();
            return sumFitness / PopulationSize;
        }

        public float GetBestFitness()
        {
            float[] fitnessValues = Population.Select(GetFitness).ToArray();
            return fitnessValues.Max();
        }

        public int GetNthBestIndex(int n)
        {
            float[] fitnessValues = Population.Select(GetFitness).ToArray();
            int[] indices = Enumerable.Range(0, PopulationSize).ToArray();
            Array.Sort(fitnessValues, indices);
            return indices[PopulationSize - n];
        }

        public void SetRewardMode(GeneratorReward mode)
        {
            this.RewardMode = mode;
        }

        public Chromosome GetBestIndividual()
        {
            int bestIndex = GetNthBestIndex(1);
            return Population[bestIndex];
        }

        private static void Shuffle<T>(T[] array)
        {
            System.Random rng = new System.Random();
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        private void InitializePopulation()
        {
            Population = new List<Chromosome>();

            for (int i = 0; i < PopulationSize; i++)
            {
                Chromosome chromosome = new Chromosome(GenerateRandomList(
                    GeneticAlgorithm.Instance.ChromosomeLength, GeneticAlgorithm.Instance.RefNumCellTypes));
                Population.Add(chromosome);
            }
        }

        private List<int> GenerateRandomList(int NumItems, int MaxValue)
        {
            List<int> randomList = new List<int>();
            System.Random random = new System.Random();

            for (int i = 0; i < NumItems; i++)
            {
                int randomValue = random.Next(0, MaxValue);
                randomList.Add(randomValue);
            }

            return randomList;
        }
            
        public class Chromosome
        {
            private List<int> genes;

            public List<int> Genes { 
                get { return genes; }
                set {
                    genes = value;
                    IsFitnessCalced = false;
                    Fitness = -float.MaxValue;
                }
            }
            public float Fitness { get; set; }
            public bool IsFitnessCalced { get; set; }

            public Chromosome(List<int> genes)
            {
                Genes = genes;
                IsFitnessCalced = false;
                Fitness = -float.MaxValue;
            }

            public Chromosome DeepCopy()
            {
                Chromosome copy = new Chromosome(Genes);
                copy.Genes = new List<int>(Genes);
                copy.Fitness = Fitness;
                copy.IsFitnessCalced = IsFitnessCalced;
                return copy;
            }
            
            public override string ToString()
            {
                string s = "";
                foreach (int gene in Genes)
                {
                    s += gene.ToString() + " ";
                }

                // Add fitness value
                s += "/ fitness: " + Fitness.ToString();

                return s;
            }

        }
        public enum SelectMethod 
        {
            RouletteWheel,
            Tournament
        }


    }

    public static class ArrayUtility
    {
        public static List<int> ToList(this int[] array)
        {
            return new List<int>(array);
        }
    }

}

