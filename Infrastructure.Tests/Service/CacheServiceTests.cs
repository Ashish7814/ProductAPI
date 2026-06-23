using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Product.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Infrastructure.Tests.Service
{
    public class InMemoryCacheServiceTests
    {
        private static InMemoryCacheService CreateSut()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            return new InMemoryCacheService(cache, NullLogger<InMemoryCacheService>.Instance);
        }

        private record TestPayload(int Id, string Name);

        // ── GetAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAsync_WhenKeyNotSet_ReturnsNull()
        {
            var sut = CreateSut();
            var result = await sut.GetAsync<TestPayload>("missing:key");
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAsync_WhenKeySet_ReturnsValue()
        {
            var sut = CreateSut();
            var payload = new TestPayload(1, "Widget");

            await sut.SetAsync("products:1", payload);
            var result = await sut.GetAsync<TestPayload>("products:1");

            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Widget");
        }

        // ── SetAsync ──────────────────────────────────────────────────────────

        [Fact]
        public async Task SetAsync_OverwritesExistingValue()
        {
            var sut = CreateSut();
            await sut.SetAsync("products:1", new TestPayload(1, "Old"));
            await sut.SetAsync("products:1", new TestPayload(1, "New"));

            var result = await sut.GetAsync<TestPayload>("products:1");
            result!.Name.Should().Be("New");
        }

        [Fact]
        public async Task SetAsync_WithShortTtl_ExpiresEntry()
        {
            var sut = CreateSut();
            await sut.SetAsync("products:ttl", new TestPayload(99, "Temp"), TimeSpan.FromMilliseconds(50));

            await Task.Delay(100); // wait for expiry

            var result = await sut.GetAsync<TestPayload>("products:ttl");
            result.Should().BeNull();
        }

        // ── RemoveAsync ───────────────────────────────────────────────────────

        [Fact]
        public async Task RemoveAsync_DeletesKey()
        {
            var sut = CreateSut();
            await sut.SetAsync("products:del", new TestPayload(5, "Delete Me"));

            await sut.RemoveAsync("products:del");

            var result = await sut.GetAsync<TestPayload>("products:del");
            result.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
        {
            var sut = CreateSut();
            var act = async () => await sut.RemoveAsync("products:ghost");
            await act.Should().NotThrowAsync();
        }

        // ── RemoveByPrefixAsync ───────────────────────────────────────────────

        [Fact]
        public async Task RemoveByPrefixAsync_ClearsAllKeysUnderPrefix()
        {
            var sut = CreateSut();
            await sut.SetAsync("products:1", new TestPayload(1, "A"));
            await sut.SetAsync("products:2", new TestPayload(2, "B"));
            await sut.SetAsync("users:1", new TestPayload(3, "C")); // different prefix

            await sut.RemoveByPrefixAsync("products");

            var r1 = await sut.GetAsync<TestPayload>("products:1");
            var r2 = await sut.GetAsync<TestPayload>("products:2");
            var r3 = await sut.GetAsync<TestPayload>("users:1");

            r1.Should().BeNull();
            r2.Should().BeNull();
            r3.Should().NotBeNull(); // unaffected
        }

        [Fact]
        public async Task RemoveByPrefixAsync_UnknownPrefix_DoesNotThrow()
        {
            var sut = CreateSut();
            var act = async () => await sut.RemoveByPrefixAsync("unknown");
            await act.Should().NotThrowAsync();
        }

        // ── Concurrency ───────────────────────────────────────────────────────

        [Fact]
        public async Task SetAsync_ConcurrentWrites_DoNotThrow()
        {
            var sut = CreateSut();
            var tasks = Enumerable.Range(1, 50)
                .Select(i => sut.SetAsync($"products:{i}", new TestPayload(i, $"P{i}")));

            var act = async () => await Task.WhenAll(tasks);
            await act.Should().NotThrowAsync();
        }
    }
}
