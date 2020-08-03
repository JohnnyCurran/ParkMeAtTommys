using System;

namespace Contracts
{
    public interface IPassToParkIt
    {
	ParkingResult ParkMyCar(ParkingInformation parkInfo);

	void ChromedriverSmokeTest();
    }
}
