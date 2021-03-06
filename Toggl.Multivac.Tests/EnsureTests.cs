﻿﻿using System;
using FluentAssertions;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class EnsureTests
    {
        public sealed class TheArgumentIsNotNullMethod
        {
            [Fact]
            public void ThrowsWhenTheArgumentIsNull()
            {
                const string argumentName = "argument";

                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNull<string>(null, argumentName);

                whenTheCalledArgumentIsNull
                    .ShouldThrow<ArgumentException>()
                    .WithMessage("Value cannot be null.\nParameter name: argument");
            }

            [Fact]
            public void DoesNotThrowWhenTheArgumentIsNotNull()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNull("something", "argument");

                whenTheCalledArgumentIsNull.ShouldNotThrow();
            }

            [Fact]
            public void WorksForValueTypes()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNull(0, "argument");

                whenTheCalledArgumentIsNull.ShouldNotThrow();
            }
        }

        public sealed class TheArgumentIsNotNullOrWhiteSpaceMethod
        {
            [Fact]
            public void ThrowsWhenTheArgumentIsAnEmptyString()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNullOrWhiteSpaceString("", "argument");

                whenTheCalledArgumentIsNull
                    .ShouldThrow<ArgumentException>()
                    .WithMessage("String cannot be empty.\nParameter name: argument");
            }

            [Fact]
            public void ThrowsWhenTheArgumentIsABlankString()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNullOrWhiteSpaceString(" ", "argument");

                whenTheCalledArgumentIsNull
                    .ShouldThrow<ArgumentException>()
                    .WithMessage("String cannot be empty.\nParameter name: argument");
            }

            [Fact]
            public void ThrowsWhenTheArgumentIsNull()
            {
                const string argumentName = "argument";

                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNullOrWhiteSpaceString(null, argumentName);

                whenTheCalledArgumentIsNull
                    .ShouldThrow<ArgumentException>()
                    .WithMessage("Value cannot be null.\nParameter name: argument");
            }

            [Fact]
            public void DoesNotThrowWhenTheArgumentIsNotNull()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsNotNullOrWhiteSpaceString("something", "argument");

                whenTheCalledArgumentIsNull.ShouldNotThrow();
            }
        }

        public sealed class TheUriIsAbsoluteMethod
        {
            [Fact]
            public void ThrowsWhenTheUriIsNotAbsolute()
            {
                const string argumentName = "argument";

                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsAbsoluteUri(new Uri("/something", UriKind.Relative), argumentName);
                
                whenTheCalledArgumentIsNull
                    .ShouldThrow<ArgumentException>()
                    .WithMessage("Uri must be absolute.\nParameter name: argument");
            }

            [Fact]
            public void DoesNotThrowWhenUriIsAbsolute()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsAbsoluteUri(new Uri("http://www.toggl.com", UriKind.Absolute), "argument");

                whenTheCalledArgumentIsNull.ShouldNotThrow();
            }

            [Fact]
            public void ThrowsIfTheUriIsNull()
            {
                Action whenTheCalledArgumentIsNull =
                    () => Ensure.Argument.IsAbsoluteUri(null, "argument");

                whenTheCalledArgumentIsNull
                    .ShouldThrow<ArgumentException>()
                    .WithMessage("Value cannot be null.\nParameter name: argument"); ;
            }
        }
    }
}