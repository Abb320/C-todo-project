using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace ToDoApp
{
    //interface
    public interface ITaskManager
    {
        void AddTask(TaskItem task);
        void RemoveTask(TaskItem task);
        void ToggleComplete(TaskItem task);
        List<TaskItem> GetTasks();
        void SaveToFile(string path);
        void LoadFromFile(string path);
    }

    //abstract
    public abstract class TaskItem
    {
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; }

        protected TaskItem(string title)
        {
            Title = title;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
        }

        public abstract string GetDisplayLabel();
    }

    //task
    public class RegularTask : TaskItem
    {
        public RegularTask(string title) : base(title) { }

        public override string GetDisplayLabel()
        {
            return IsCompleted ? $"[Done] {Title}" : Title;
        }
    }

    //important task
    public class ImportantTask : TaskItem
    {
        public string Priority { get; set; }

        public ImportantTask(string title, string priority = "High") : base(title)
        {
            Priority = priority;
        }

        public override string GetDisplayLabel()
        {
            string prefix = Priority == "High" ? "[!!!]" : "[!]";
            string status = IsCompleted ? "[Done] " : "";
            return $"{status}{prefix} {Title}";
        }
    }

    //manager
    public class TaskManager : ITaskManager
    {
        private List<TaskItem> _tasks = new List<TaskItem>();

        public void AddTask(TaskItem task) => _tasks.Add(task);
        public void RemoveTask(TaskItem task) => _tasks.Remove(task);
        public List<TaskItem> GetTasks() => _tasks;

        public void ToggleComplete(TaskItem task)
        {
            task.IsCompleted = !task.IsCompleted;
        }

        public void SaveToFile(string path)
        {
            var data = _tasks.Select(t => new
            {
                Type = t.GetType().Name,
                t.Title,
                t.IsCompleted,
                t.CreatedAt,
                Priority = (t is ImportantTask it) ? it.Priority : null
            }).ToList();

            File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }

        public void LoadFromFile(string path)
        {
            if (!File.Exists(path)) return;

            var data = JsonSerializer.Deserialize<List<JsonElement>>(File.ReadAllText(path));
            if (data == null) return;

            _tasks.Clear();
            foreach (var item in data)
            {
                string type = item.GetProperty("Type").GetString() ?? "RegularTask";
                string title = item.GetProperty("Title").GetString() ?? "";
                bool completed = item.GetProperty("IsCompleted").GetBoolean();
                DateTime created = item.GetProperty("CreatedAt").GetDateTime();

                TaskItem task = type == "ImportantTask"
                    ? new ImportantTask(title, item.GetProperty("Priority").GetString() ?? "High")
                    : new RegularTask(title);

                task.IsCompleted = completed;
                task.CreatedAt = created;
                _tasks.Add(task);
            }
        }
    }

    //winform
    public class Form1 : Form
    {
        private readonly ITaskManager _taskManager = new TaskManager();
        private readonly string _saveFile = "tasks.json";

        private TextBox txtInput;
        private CheckBox chkHighPriority;
        private Button btnAdd;
        private Button btnAddImportant;
        private Button btnToggleDone;
        private Button btnDelete;
        private ListBox listBoxTasks;

        public Form1()
        {
            BuildUI();
            _taskManager.LoadFromFile(_saveFile);
            RefreshList();
        }

        private void BuildUI()
        {
            this.Text = "To-Do List";
            this.ClientSize = new Size(490, 450);

            var lblInput = new Label { Text = "Task:", Location = new Point(12, 15), Size = new Size(40, 23) };
            txtInput = new TextBox { Location = new Point(55, 12), Size = new Size(250, 23) };

            btnAdd = new Button { Text = "Add Task", Location = new Point(12, 45), Size = new Size(100, 30) };
            btnAdd.Click += (s, e) => AddTask(important: false);

            btnAddImportant = new Button { Text = "Add Important", Location = new Point(120, 45), Size = new Size(115, 30) };
            btnAddImportant.Click += (s, e) => AddTask(important: true);

            btnToggleDone = new Button { Text = "Toggle Done", Location = new Point(12, 85), Size = new Size(100, 30) };
            btnToggleDone.Click += (s, e) => ActOnSelected(task => { _taskManager.ToggleComplete(task); });

            btnDelete = new Button { Text = "Delete", Location = new Point(120, 85), Size = new Size(100, 30) };
            btnDelete.Click += (s, e) => ActOnSelected(task => { _taskManager.RemoveTask(task); });

            listBoxTasks = new ListBox { Location = new Point(12, 125), Size = new Size(460, 300) };

            this.Controls.AddRange(new Control[] {
                lblInput, txtInput, chkHighPriority,
                btnAdd, btnAddImportant, btnToggleDone, btnDelete,
                listBoxTasks
            });
        }

        private void AddTask(bool important)
        {
            string title = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(title)) return;

            TaskItem task = important
                ? new ImportantTask(title, "High")
                : new RegularTask(title);

            _taskManager.AddTask(task);
            txtInput.Clear();
            Save();
        }

        private void ActOnSelected(Action<TaskItem> action)
        {
            int index = listBoxTasks.SelectedIndex;
            if (index < 0) return;
            action(_taskManager.GetTasks()[index]);
            Save();
        }

        private void Save()
        {
            _taskManager.SaveToFile(_saveFile);
            RefreshList();
        }

        private void RefreshList()
        {
            listBoxTasks.Items.Clear();
            foreach (var task in _taskManager.GetTasks())
                listBoxTasks.Items.Add(task.GetDisplayLabel());
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new Form1());
        }
    }
}