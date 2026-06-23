using FluentAssertions;
using Product.Application.DTOs;
using Product.Application.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Application.Tests.Validators
{
    public class ValidatorTests
    {
        [Theory]
        [InlineData("", 0)]
        [InlineData(null, 0)]
        public async Task CreateProductRequestValidator_InvalidName_ShouldFail(string name, int qty)
        {
            var validator = new CreateProductRequestValidator();
            var request = new CreateProductRequest(name, qty);
            var result = await validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task CreateProductRequestValidator_ValidRequest_ShouldPass()
        {
            var validator = new CreateProductRequestValidator();
            var request = new CreateProductRequest("Valid Product", 10);
            var result = await validator.ValidateAsync(request);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task CreateProductRequestValidator_NegativeQuantity_ShouldFail()
        {
            var validator = new CreateProductRequestValidator();
            var request = new CreateProductRequest("Product", -1);
            var result = await validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RegisterRequestValidator_WeakPassword_ShouldFail()
        {
            var validator = new RegisterRequestValidator();
            var request = new RegisterRequest("user1", "user@test.com", "weak");
            var result = await validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task RegisterRequestValidator_ValidRequest_ShouldPass()
        {
            var validator = new RegisterRequestValidator();
            var request = new RegisterRequest("user1", "user@test.com", "StrongPass1");
            var result = await validator.ValidateAsync(request);
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public async Task RegisterRequestValidator_InvalidEmail_ShouldFail()
        {
            var validator = new RegisterRequestValidator();
            var request = new RegisterRequest("user1", "not-an-email", "StrongPass1");
            var result = await validator.ValidateAsync(request);
            result.IsValid.Should().BeFalse();
        }
    }
}
