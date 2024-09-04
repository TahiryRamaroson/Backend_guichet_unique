using Microsoft.ML.Data;

namespace Backend_guichet_unique.Models
{
	public class PlaintePrediction : PlainteModel
	{
		public float[] Score { get; set; }
		public uint PredictedLabel { get; set; }
	}
}
