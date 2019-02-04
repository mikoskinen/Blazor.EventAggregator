using System.Threading.Tasks;
using EventAggregator.Blazor;
using Microsoft.AspNetCore.Components;

namespace CodeBehind.App.Pages
{
    public class CounterComponent : ComponentBase
    {
        [Inject]
        private IEventAggregator _eventAggregator { get; set; }

        public int currentCount = 0;

        public async Task IncrementCountAsync()
        {
            currentCount++;
            await _eventAggregator.PublishAsync(new CounterIncreasedMessage());
        }
    }

    public class CounterIncreasedMessage
    {
    }
}
