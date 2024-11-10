using LegendaryAop;
using System.Diagnostics;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
        }
        
        public Task<int> Log(string str)
        {
            Debug.WriteLine(str);
            return Task.FromResult(str.GetHashCode());
        }
        [Log]
        private async void button1_Click(object sender, EventArgs e)
        {
            await Log("按按钮了");
        }
    }
    public class LogAttribute : AsyncAopAttribute
    {
        public override string Name => "日志";

        public override async Task<object?> InvokeAsync(IAopMetaData data)
        {
            Debug.WriteLine("记录日志");
            var ret = await data.NextAsync();
            Debug.WriteLine("记录日志后");
            return ret;
        }
    }
}
