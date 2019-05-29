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
        private static Guid uid = Guid.NewGuid(); 
        private static EventWaitHandle _unloadedEvent = 
            new EventWaitHandle(false, EventResetMode.ManualReset);

        static void Main(string[] args)
        {
            WorkflowApplication wfApp = CreateWorkflowApplication();
            var workflowInstanceId = wfApp.Id;
            wfApp.Run();
            string bookmarkName = workflowInstanceId.ToString();
            //wfApp.Unload();
            _unloadedEvent.WaitOne();
            /* create */
            wfApp = CreateWorkflowApplication();
            wfApp.Load(workflowInstanceId);
            var result = wfApp.ResumeBookmark(bookmarkName, null); // <- this hits the BookmarkActivity while **IT SHOULD'NT**

            Console.ReadKey();
        }
        public static WorkflowApplication CreateWorkflowApplication()
        {
            Activity wf = CreateWorkflow();
            WorkflowApplication result = new WorkflowApplication(wf)
            {
                InstanceStore = new XmlWorkflowInstanceStore(uid),
                Unloaded = e =>
                {
                    _unloadedEvent.Set();
                },
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
                Guid wfInstanceId = context.WorkflowInstanceId;
                string bookmarkName = wfInstanceId.ToString();

                Console.WriteLine("BookmarkActivity.Execute - creating bookmark with name {0}", bookmarkName);
                context.CreateBookmark(bookmarkName, new BookmarkCallback(BookmarkCallback), BookmarkOptions.MultipleResume);
            }

            protected override bool CanInduceIdle => true;

            private void BookmarkCallback(NativeActivityContext context, Bookmark bookmark, object bookmarkData)
            {
                Console.WriteLine($"Bookmark {bookmark.Name} resumed with data that is not a string");
            }
        }
    }
}
