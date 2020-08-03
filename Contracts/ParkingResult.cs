using System;

namespace Contracts
{
    public class ParkingResult
    {
	public bool Success { get; set; }
	public string ErrorMessage { get; set; }
	public string ParkingPassLocation { get; set; }
    }
}
