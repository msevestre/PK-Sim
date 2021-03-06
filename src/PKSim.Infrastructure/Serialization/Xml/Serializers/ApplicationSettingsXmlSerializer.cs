using System.Xml.Linq;
using OSPSuite.Utility.Container;
using PKSim.Core;
using OSPSuite.Core.Serialization.Xml;

namespace PKSim.Infrastructure.Serialization.Xml.Serializers
{
   public class ApplicationSettingsXmlSerializer : BaseXmlSerializer<IApplicationSettings>
   {
      public ApplicationSettingsXmlSerializer()
      {
         ElementName = CoreConstants.Serialization.ApplicationSettings;
      }

      public override void PerformMapping()
      {
         MapEnumerable(x => x.SpeciesDataBaseMaps, x => x.AddSpeciesDatabaseMap);
      }

      public override IApplicationSettings CreateObject(XElement element, SerializationContext serializationContext)
      {
         return IoC.Resolve<IApplicationSettings>();
      }
   }
}