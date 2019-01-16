using LedStripServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace LedStripServer.Controllers
{
    public class LedController : Controller
    {
        private readonly SoftwarePwmController _pwmController;

        public LedController(SoftwarePwmController pwmController) =>
            _pwmController = pwmController;

        [Route("/set/{red}/{green}/{blue}", Name = Routes.SetLedColor)]
        public IActionResult SetLedColor(int red, int green, int blue, double hz = 15.0)
        {
            _pwmController.SetPinPwm(17, hz, red / 255.0);
            _pwmController.SetPinPwm(22, hz, green / 255.0);
            _pwmController.SetPinPwm(24, hz, blue / 255.0);

            return Ok();
        }
    }
}
