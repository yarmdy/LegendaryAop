using LegendaryAop;
using System.Diagnostics;

namespace WinFormsTest
{
    public partial class Form1 : Form
    {
        public string GG { [Log]get; set; }
        public Form1()
        {
            GG = "����GG��ֵ";
            InitializeComponent();
            var gg = GG;
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
            Debug.WriteLine($"��¼��־{data.Method.DeclaringType}::{data.Method}:{data.IsGetMethod}");
            var ret = await data.NextAsync();
            Debug.WriteLine($"��¼��־��{ret}");
            if(data.Obj is Form form)
            {
                form.Text = ret + "" ;
            }
            return ret;
        }
    }
}
