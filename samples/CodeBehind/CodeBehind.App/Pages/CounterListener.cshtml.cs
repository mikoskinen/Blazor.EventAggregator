using System.Threading.Tasks;
using EventAggregator.Blazor;
using Microsoft.AspNetCore.Components;

namespace CodeBehind.App.Pages
{
    public class CounterListenerComponent : ComponentBase, IHandle<CounterIncreasedMessage>
    {
        [Inject]
        private IEventAggregator _eventAggregator { get; set; }

        public int currentCount = 0;

        protected override void OnInit()
        {
            _eventAggregator.Subscribe(this);
        }

        public Task HandleAsync(CounterIncreasedMessage message)
        {
            currentCount += 1;
            return Task.CompletedTask;
        }
    }
}
