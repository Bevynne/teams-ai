﻿using System.Reflection;
using Microsoft.Teams.AI.AI;
using Microsoft.Teams.AI.AI.Action;
using Microsoft.Teams.AI.AI.Planners;
using Microsoft.Teams.AI.Exceptions;
using Microsoft.Teams.AI.State;
using Microsoft.Teams.AI.Tests.TestUtils;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Bot.Builder;
using TestTurnState = Microsoft.Teams.AI.Tests.TestUtils.TestTurnState;
using Record = Microsoft.Teams.AI.State.Record;
using Microsoft.Teams.AI.AI.Moderator;

namespace Microsoft.Teams.AI.Tests.AITests
{
    public class DefaultActionsTests
    {
        [Fact]
        public void Test_DefaultActions_Are_Imported()
        {
            // Act
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>();

            // Assert
            Assert.True(actions.ContainsAction(AIConstants.UnknownActionName));
            Assert.True(actions.ContainsAction(AIConstants.FlaggedInputActionName));
            Assert.True(actions.ContainsAction(AIConstants.FlaggedOutputActionName));
            Assert.True(actions.ContainsAction(AIConstants.HttpErrorActionName));
            Assert.True(actions.ContainsAction(AIConstants.PlanReadyActionName));
            Assert.True(actions.ContainsAction(AIConstants.DoCommandActionName));
            Assert.True(actions.ContainsAction(AIConstants.SayCommandActionName));
            Assert.True(actions.ContainsAction(AIConstants.TooManyStepsActionName));
        }

        [Fact]
        public async Task Test_Execute_UnknownAction()
        {
            // Arrange
            var logs = new List<string>();
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>(logs);
            var activity = MessageFactory.Text("hello");
            var turnContext = new TurnContext(new NotImplementedAdapter(), activity);
            var turnState = new TestTurnState();

            // Act
            var unknownAction = actions[AIConstants.UnknownActionName];
            var result = await unknownAction.Handler.PerformAction(turnContext, turnState, null, "test-action");

            // Assert
            Assert.Equal(AIConstants.StopCommand, result);
            Assert.Equal(1, logs.Count);
            Assert.Equal("An AI action named \"test-action\" was predicted but no handler was registered", logs[0]);
        }

        [Fact]
        public async Task Test_Execute_FlaggedInputAction()
        {
            // Arrange
            var logs = new List<string>();
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>(logs);
            var activity = MessageFactory.Text("hello");
            var turnContext = new TurnContext(new NotImplementedAdapter(), activity);
            var turnState = new TestTurnState();

            // Act
            var flaggedInputAction = actions[AIConstants.FlaggedInputActionName];
            var result = await flaggedInputAction.Handler.PerformAction(turnContext, turnState, null, null);

            // Assert
            Assert.Equal(AIConstants.StopCommand, result);
            Assert.Equal(1, logs.Count);
            Assert.Equal("The users input has been moderated but no handler was registered for ___FlaggedInput___", logs[0]);
        }

        [Fact]
        public async Task Test_Execute_FlaggedOutputAction()
        {
            // Arrange
            var logs = new List<string>();
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>(logs);
            var activity = MessageFactory.Text("hello");
            var turnContext = new TurnContext(new NotImplementedAdapter(), activity);
            var turnState = new TestTurnState();

            // Act
            var flaggedOutputAction = actions[AIConstants.FlaggedOutputActionName];
            var result = await flaggedOutputAction.Handler.PerformAction(turnContext, turnState, null, null);

            // Assert
            Assert.Equal(AIConstants.StopCommand, result);
            Assert.Equal(1, logs.Count);
            Assert.Equal("The bots output has been moderated but no handler was registered for ___FlaggedOutput___", logs[0]);
        }

        [Fact]
        public async Task Test_Execute_HttpErrorAction()
        {
            // Arrange
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>();
            var activity = MessageFactory.Text("hello");
            var turnContext = new TurnContext(new NotImplementedAdapter(), activity);
            var turnState = new TestTurnState();

            // Act
            var httpErrorAction = actions[AIConstants.HttpErrorActionName];
            var exception = await Assert.ThrowsAsync<TeamsAIException>(async () => await httpErrorAction.Handler.PerformAction(turnContext, turnState, null, null));

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("An AI http request failed", exception.Message);
        }

        [Fact]
        public async Task Test_Execute_PlanReadyAction()
        {
            // Arrange
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>();
            var activity = MessageFactory.Text("hello");
            var turnContext = new TurnContext(new NotImplementedAdapter(), activity);
            var turnState = new TestTurnState();
            var plan0 = new Plan(new List<IPredictedCommand>());
            var plan1 = new Plan(new List<IPredictedCommand>()
            {
                new PredictedDoCommand("action"),
            });

            // Act
            var planReadyAction = actions[AIConstants.PlanReadyActionName];
            var result0 = await planReadyAction.Handler.PerformAction(turnContext, turnState, plan0, null);
            var result1 = await planReadyAction.Handler.PerformAction(turnContext, turnState, plan1, null);
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await planReadyAction.Handler.PerformAction(turnContext, turnState, null, null));

            // Assert
            Assert.Equal(AIConstants.StopCommand, result0);
            Assert.Equal(string.Empty, result1);
            Assert.NotNull(exception);
            Assert.Equal("Value cannot be null. (Parameter 'plan')", exception.Message);
        }

        [Fact]
        public async Task Test_Execute_DoCommandAction()
        {
            // Arrange
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>();
            var activity = MessageFactory.Text("hello");
            var turnContext = new TurnContext(new NotImplementedAdapter(), activity);
            var turnState = new TestTurnState();
            var handler = new TestActionHandler();
            var data = new DoCommandActionData<TestTurnState>
            {
                PredictedDoCommand = new PredictedDoCommand("test-action"),
                Handler = handler,
            };

            // Act
            var doCommandAction = actions[AIConstants.DoCommandActionName];
            var result = await doCommandAction.Handler.PerformAction(turnContext, turnState, data, null);
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await doCommandAction.Handler.PerformAction(turnContext, turnState, null, null));

            // Assert
            Assert.Equal("test-result", result);
            Assert.Equal("test-action", handler.ActionName);
            Assert.NotNull(exception);
            Assert.Equal("Value cannot be null. (Parameter 'doCommandActionData')", exception.Message);
        }

        [Fact]
        public async Task Test_Execute_SayCommandAction()
        {
            // Arrange
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>();
            var turnContextMock = new Mock<ITurnContext>();
            turnContextMock.Setup(tc => tc.Activity).Returns(new Activity { Type = ActivityTypes.Message });
            turnContextMock.Setup(tc => tc.SendActivityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new ResourceResponse()));
            var turnState = new TestTurnState();
            var command = new PredictedSayCommand("hello");

            // Act
            var sayCommandAction = actions[AIConstants.SayCommandActionName];
            var result = await sayCommandAction.Handler.PerformAction(turnContextMock.Object, turnState, command, null);
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await sayCommandAction.Handler.PerformAction(turnContextMock.Object, turnState, null, null));

            // Assert
            Assert.Equal(string.Empty, result);
            turnContextMock.Verify(tc => tc.SendActivityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.NotNull(exception);
            Assert.Equal("Value cannot be null. (Parameter 'command')", exception.Message);
        }

        [Fact]
        public async Task Test_Execute_TooManyStepsAction()
        {
            // Arrange
            IActionCollection<TestTurnState> actions = ImportDefaultActions<TestTurnState>();
            var turnContextMock = new Mock<ITurnContext>();
            var turnState = new TestTurnState();
            var tooManyStepsParameters1 = new TooManyStepsParameters(25, TimeSpan.Zero, DateTime.UtcNow, 30);
            var tooManyStepsParameters2 = new TooManyStepsParameters(25, TimeSpan.Zero, DateTime.UtcNow, 20);

            // Act
            var tooManyStepsAction = actions[AIConstants.TooManyStepsActionName];
            var exception1 = await Assert.ThrowsAsync<TeamsAIException>(async () => await tooManyStepsAction.Handler.PerformAction(turnContextMock.Object, turnState, tooManyStepsParameters1, null));
            var exception2 = await Assert.ThrowsAsync<TeamsAIException>(async () => await tooManyStepsAction.Handler.PerformAction(turnContextMock.Object, turnState, tooManyStepsParameters2, null));

            // Assert
            Assert.NotNull(exception1);
            Assert.Equal("The AI system has exceeded the maximum number of steps allowed.", exception1.Message);
            Assert.NotNull(exception1);
            Assert.Equal("The AI system has exceeded the maximum amount of time allowed.", exception2.Message);
        }

        private static IActionCollection<TState> ImportDefaultActions<TState>(List<string>? logs = null) where TState : ITurnState<Record, Record, TempState>
        {
            ILogger? logger = null;
            if (logs != null)
            {
                Mock<ILogger> loggerMock = new();
                loggerMock.Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                    .Callback(new InvocationAction(invocation =>
                    {
                        var state = invocation.Arguments[2];
                        var exception = (Exception)invocation.Arguments[3];
                        var formatter = invocation.Arguments[4];

                        var invokeMethod = formatter.GetType().GetMethod("Invoke");
                        var logMessage = (string?)invokeMethod?.Invoke(formatter, new[] { state, exception });
                        if (logMessage != null)
                        {
                            logs.Add(logMessage);
                        }
                    }));
                logger = loggerMock.Object;
            }
            ILoggerFactory loggerFactory = new TestLoggerFactory(logger);

            AIOptions<TState> options = new(
                new Mock<IPlanner<TState>>().Object,
                new Mock<IModerator<TState>>().Object);
            AI<TState> ai = new(options, loggerFactory);
            // get _actions field from AI class
            FieldInfo actionsField = typeof(AI<TState>).GetField("_actions", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance)!;
            return (IActionCollection<TState>)actionsField!.GetValue(ai)!;
        }
    }
}
