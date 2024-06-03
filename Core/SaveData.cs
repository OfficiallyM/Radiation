using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Radiation.Core
{
	[DataContract]
	internal class PoisonData
	{
		[DataMember] public int Id { get; set; }
		[DataMember] public float RadiationLevel { get; set; } = 0;
		[DataMember] public float RadAway { get; set; } = 0;
		[DataMember] public bool IsNPCTransformed { get; set; } = false;

	}

	[DataContract]
	internal class SaveData
	{
		[DataMember] public List<PoisonData> PoisonData { get; set; }
		[DataMember] public bool HasFoundGeigerCounter { get; set; }
	}
}
