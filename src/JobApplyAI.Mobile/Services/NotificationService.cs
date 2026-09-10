using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;

namespace JobApplyAI.Mobile.Services;

/// <summary>
/// Thin wrapper over Plugin.LocalNotification for on-device notifications - AI generation
/// completion (tailored resume, cover letter, interview feedback) and interview-prep reminders.
/// This is local-only (no server push): true cross-device push requires an Azure Notification
/// Hub plus registered APNs (iOS) and FCM (Android) credentials, which aren't available in this
/// environment. Local notifications still give a real "your result is ready" nudge while the
/// app is backgrounded, without any extra infrastructure.
/// </summary>
public static class NotificationService
{
    private static int _nextId = 1000;

    public static async Task NotifyAsync(string title, string message)
    {
        try
        {
            var granted = await LocalNotificationCenter.Current.RequestNotificationPermission();
            if (!granted) return;

            var request = new NotificationRequest
            {
                NotificationId = _nextId++,
                Title = title,
                Description = message,
                Schedule = new NotificationRequestSchedule { NotifyTime = DateTime.Now.AddSeconds(1) }
            };
            await LocalNotificationCenter.Current.Show(request);
        }
        catch
        {
            // Notifications are a nice-to-have - never let a permission/platform failure break a flow.
        }
    }
}
