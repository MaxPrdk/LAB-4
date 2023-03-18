using System;
using System.Collections.Generic;

namespace EventDrivenWorkflowEngine
{
    public enum WorkflowStatus
    {
        InProgress,
        Completed,
        Failed
    }

    public class WorkflowEventArgs : EventArgs
    {
        public WorkflowStatus Status { get; set; }
    }

    public interface IWorkflowAction
    {
        string Name { get; }
        void Execute();
    }

    public class ActionA : IWorkflowAction
    {
        public string Name => "Action A";

        public void Execute()
        {
            Console.WriteLine("Executing Action A");
        }
    }

    public class ActionB : IWorkflowAction
    {
        public string Name => "Action B";

        public void Execute()
        {
            Console.WriteLine("Executing Action B");
        }
    }

    public class ActionC : IWorkflowAction
    {
        public string Name => "Action C";

        public void Execute()
        {
            Console.WriteLine("Executing Action C");
        }
    }

    public class WorkflowEngine
    {
        private readonly Dictionary<IWorkflowAction, List<IWorkflowAction>> _workflowTransitions;
        private readonly Dictionary<IWorkflowAction, EventHandler<WorkflowEventArgs>> _workflowEventHandlers;
        private IWorkflowAction _currentAction;
        private WorkflowStatus _status;

        public WorkflowEngine()
        {
            _workflowTransitions = new Dictionary<IWorkflowAction, List<IWorkflowAction>>();
            _workflowEventHandlers = new Dictionary<IWorkflowAction, EventHandler<WorkflowEventArgs>>();
        }

        public void RegisterTransition(IWorkflowAction fromAction, IWorkflowAction toAction)
        {
            if (!_workflowTransitions.ContainsKey(fromAction))
            {
                _workflowTransitions[fromAction] = new List<IWorkflowAction>();
            }

            _workflowTransitions[fromAction].Add(toAction);
        }

        public void RegisterEventHandler(IWorkflowAction action, EventHandler<WorkflowEventArgs> eventHandler)
        {
            _workflowEventHandlers[action] += eventHandler;
        }

        public void UnregisterEventHandler(IWorkflowAction action, EventHandler<WorkflowEventArgs> eventHandler)
        {
            _workflowEventHandlers[action] -= eventHandler;
        }

        public void Start(IWorkflowAction startingAction)
        {
            _currentAction = startingAction;
            _status = WorkflowStatus.InProgress;
            ExecuteCurrentAction();
        }

        private void ExecuteCurrentAction()
        {
            Console.WriteLine($"Current action: {_currentAction.Name}");

            _currentAction.Execute();

            var eventArgs = new WorkflowEventArgs { Status = _status };

            if (_workflowEventHandlers.ContainsKey(_currentAction))
            {
                _workflowEventHandlers[_currentAction]?.Invoke(this, eventArgs);
            }

            if (_status == WorkflowStatus.InProgress)
            {
                var nextActions = GetNextActions();
                if (nextActions.Count == 0)
                {
                    _status = WorkflowStatus.Completed;
                    Console.WriteLine("Workflow completed");
                    return;
                }

                _currentAction = nextActions[0];
                ExecuteCurrentAction();
            }
            else
            {
                Console.WriteLine($"Workflow failed with status: {_status}");
            }
        }

        private List<IWorkflowAction> GetNextActions()
        {
            if (_workflowTransitions.ContainsKey(_currentAction))
            {
                return _workflowTransitions[_currentAction];
            }

            return new List<IWorkflowAction>();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var engine = new WorkflowEngine();

            var actionA = new ActionA();
            var actionB = new ActionB();
            var actionC = new ActionC();

            engine.RegisterTransition(actionA, actionB);
            engine.RegisterTransition(action
