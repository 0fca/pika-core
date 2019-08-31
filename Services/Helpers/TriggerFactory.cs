using Quartz;

namespace PikaCore.Services.Helpers
{
    public class TriggerFactory
    {
        private static readonly TriggerFactory _triggerFactory = new TriggerFactory();

        private TriggerFactory() { }

        public static TriggerFactory GetInstance()
        {
            return _triggerFactory;
        }

        public ITrigger CreateTrigger(TriggerData triggerData)
        {
            var triggerBuilder = TriggerBuilder.Create();

            if (triggerData.TriggerIdentity != null)
            {
                triggerBuilder.WithIdentity(triggerData.TriggerIdentity.Name, triggerData.TriggerIdentity.Group);
            }

            if (triggerData.ScheduleBuilder != null)
            {
                triggerBuilder.WithSchedule(triggerData.ScheduleBuilder);
            }

            if (triggerData.ShouldStartNow)
            {
                triggerBuilder.StartNow();
            }
            else
            {
                triggerBuilder.StartAt(triggerData.When);
            }
            return triggerBuilder.Build();
        }
    }
}
