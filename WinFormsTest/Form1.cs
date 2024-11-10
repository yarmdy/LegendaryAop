using LegendaryAop;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var asd = Log("的方法 ").Result;
        }
        [Log]
        public Task<int> Log(string str)
        {
            Console.WriteLine(str);
            return Task.FromResult(str.GetHashCode());
        }
    }
    public class LogAttribute : AsyncAopAttribute
    {
        public override string Name => "日志";

        public override async Task<object?> InvokeAsync(IAopMetaData data)
        {
            return await data.NextAsync();
        }
    }
}
