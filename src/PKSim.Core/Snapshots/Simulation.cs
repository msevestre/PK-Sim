﻿namespace PKSim.Core.Snapshots
{
   public class Simulation : SnapshotBase
   {
      public SimulationConfiguration Configuration { get; set; }
      public string[] ObservedData { get; set; }
      public SolverSettings Solver { get; set; }
      public OutputSchema OutputSchema { get; set; }
      public LocalizedParameter[] Parameters { get; set; }
      public OutputSelections OutputSelections { get; set; }
      public string Individual { get; set; }
      public string Population { get; set; }
      public CompoundProperties[] Compounds { get; set; }
      public EventSelection[] Events { get; set; }
      public AdvancedParameter[] AdvancedParameters { get; set; }

   }
}