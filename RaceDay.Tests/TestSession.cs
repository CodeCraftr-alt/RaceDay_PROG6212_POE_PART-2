using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace RaceDay.Tests
{
    // A small session used when calling a controller directly in a unit test.
    // The HTTP tests use ASP.NET Core's real session instead.
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new Dictionary<string, byte[]>();

        public string Id { get; } = Guid.NewGuid().ToString();
        public bool IsAvailable { get; } = true;
        public IEnumerable<string> Keys
        {
            get { return _values.Keys; }
        }

        public void Clear()
        {
            _values.Clear();
        }

        public void Remove(string key)
        {
            _values.Remove(key);
        }

        public void Set(string key, byte[] value)
        {
            _values[key] = value;
        }

        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
        {
            return _values.TryGetValue(key, out value);
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}

