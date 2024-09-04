using Microsoft.ML.Data;

namespace Backend_guichet_unique.Models
{
	public class PlainteModel
	{
		[LoadColumn(0)]
		public string Description { get; set; }
		[LoadColumn(1)]
		public uint Label { get; set; }
	}
}
