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
        public override string Name => "»’÷æ";

        public override async Task<object?> InvokeAsync(IAopMetaData data)
        {
            return await data.NextAsync();
        }
    }
}
