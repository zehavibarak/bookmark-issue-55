using System;
using System.Activities;
using System.Activities.Statements;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bookmark_issue_55
{
    class Program
    {
        private static readonly AutoResetEvent _unloadedEvent = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            var wfApp = CreateWorkflowApplication();

            wfApp.Run();
            var workflowInstanceId = wfApp.Id;
            _unloadedEvent.WaitOne();
            /* new scope */
            wfApp = CreateWorkflowApplication();
            wfApp.Load(workflowInstanceId);

            Console.WriteLine("copy guid above here");
            string bookmarkName = Console.ReadLine();

            var result = wfApp.ResumeBookmark(bookmarkName, null);

            if (result != BookmarkResumptionResult.Success)
            {
                Console.WriteLine("Failed!");
            }

            Console.ReadKey();
        }
        public static WorkflowApplication CreateWorkflowApplication()
        {
            Activity wf = CreateWorkflow();
            WorkflowApplication result = new WorkflowApplication(wf)
            {
                InstanceStore = new FileInstanceStore("."),
                Unloaded = e => _unloadedEvent.Set(),
                PersistableIdle = e => PersistableIdleAction.Unload
            };

            return result;
        }

        public static Activity CreateWorkflow()
        {
            Sequence workflow = new Sequence();
            workflow.Activities.Add(
                new WriteLine
                {
                    Text = "Before Bookmark"
                });
            workflow.Activities.Add(new BookmarkActivity());
            workflow.Activities.Add(
                new WriteLine
                {
                    Text = "After Bookmark"
                });

            return workflow;
        }



        public class BookmarkActivity : NativeActivity
        {
            protected override void Execute(NativeActivityContext context)
            {
                string bookmarkName = Guid.NewGuid().ToString();

                context.CreateBookmark(bookmarkName, new BookmarkCallback(BookmarkCallback), BookmarkOptions.MultipleResume);
                Console.WriteLine("BookmarkActivity.Execute - successfull created bookmark with name {0}", bookmarkName);
            }

            protected override bool CanInduceIdle => true;

            private void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object bookmarkData)
            {
                Console.WriteLine($"Bookmark {bookmark.Name} resumed.");
            }
        }
    }
}
