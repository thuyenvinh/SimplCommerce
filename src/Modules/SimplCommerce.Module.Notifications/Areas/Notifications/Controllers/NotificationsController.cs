using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SimplCommerce.Module.Notifications.Areas.Notifications.ViewModels;
using SimplCommerce.Module.Notifications.Models;
using SimplCommerce.Module.Notifications.Notifiers;

namespace SimplCommerce.Module.Notifications.Areas.Notifications.Controllers
{
    [Area("Notifications")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class NotificationsController : Controller
    {
        private readonly ITestNotifier _testNotifier;

        public NotificationsController(ITestNotifier testNotifier)
        {
            _testNotifier = testNotifier;
        }

        [HttpGet("notifications")]
        public IActionResult Index()
        {
            return View();
        }

        #region Etc
        [HttpPost]
        public async Task<ActionResult> TestNotification(TestNotificationVm inputDto)
        {
            if (string.IsNullOrEmpty(inputDto.Message))
            {
                inputDto.Message = "This is a test notification, created at " + DateTime.Now;
            }

            var severity = Enum.TryParse<NotificationSeverity>(inputDto.Severity, ignoreCase: true, out var s)
                ? s
                : NotificationSeverity.Info;

            await _testNotifier.SendMessageAsync(inputDto.UserId, inputDto.Message, severity);

            return RedirectToAction("Index");
        }

        #endregion
    }
}
