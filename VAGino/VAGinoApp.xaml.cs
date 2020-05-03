using Microsoft.Toolkit.Helpers;

using VAGino.Services;

namespace VAGino
{
    partial class App
    {
        partial void Construct()
        {
            Singleton<DBService>.Instance.Init();
        }
    }
}
