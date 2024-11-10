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
            await Log("����ť��");
        }
    }
    public class LogAttribute : AsyncAopAttribute
    {
        public override string Name => "��־";

        public override async Task<object?> InvokeAsync(IAopMetaData data)
        {
            Debug.WriteLine("��¼��־");
            var ret = await data.NextAsync();
            Debug.WriteLine("��¼��־��");
            return ret;
        }
    }
}
