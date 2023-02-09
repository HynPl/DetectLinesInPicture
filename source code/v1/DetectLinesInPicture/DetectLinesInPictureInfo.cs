using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace DetectLinesInPicture {
    public class DetectLinesInPictureInfo : GH_AssemblyInfo {
        public override string Name => "DetectLinesInPicture";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => Properties.Resources.detect;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "Detekce čar v obrázku\nDetect lines on the bitmap image";

        public override Guid Id => new Guid("867DB856-B565-4349-86D9-53EA34A33F60");

        //Return a string identifying you or your company.
        public override string AuthorName => "Hynek Pluskal";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "hynekopluskal@seznam.cz; pluskhyn@cvut.cz";
    }
}