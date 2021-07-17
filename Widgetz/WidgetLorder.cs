using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Widgetz.Interface;

namespace Widgetz {
    public class WidgetLorder {
        public static IEnumerable<IWidget> GetWidgets(string path) {
            var widgets = new List<IWidget>();
            foreach(var dll in Directory.GetFiles(path, "*.dll")) {
                var assembly = Assembly.LoadFrom(dll);
                foreach(var type in assembly.GetTypes()) {
                    // 非クラス型、非パブリック型、抽象クラスはスキップ
                    if(!type.IsClass || !type.IsPublic || type.IsAbstract) {
                        continue;
                    }
                    var t = type.GetInterfaces().FirstOrDefault((t) => t == typeof(IWidget));
                    if(t == default(IWidget)) {
                        continue;
                    }
                    // 取得した型のインスタンスを作成
                    var obj = Activator.CreateInstance(type);
                    widgets.Add((IWidget)obj);
                }
            }
            return widgets;
        }
    }
}
