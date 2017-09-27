﻿using OSPSuite.Core.Domain;
using PKSim.Core.Model;

namespace PKSim.Core.Snapshots
{
   public class Molecule : ParameterContainerSnapshotBase
   {
      public QuantityType Type { get; set; }

      //Proteins only
      public MembraneLocation? MembraneLocation { get; set; }
      public TissueLocation? TissueLocation { get; set; }
      public IntracellularVascularEndoLocation? IntracellularVascularEndoLocation { get; set; }

      //Transporters only
      public TransportType? TransportType { get; set; }

      public LocalizedParameter[] Expression { get; set; }
      public Ontogeny Ontogeny { get; set; }
   }
}