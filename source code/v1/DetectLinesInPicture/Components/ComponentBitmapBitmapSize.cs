using Grasshopper.Kernel;
using System;

namespace DetectLinesInPicture {
    public class ComponentBitmapSize : GH_Component {

        public ComponentBitmapSize()
          : base("Bitmap size", "Bitmap size",
            "Get bitmap width & height",
            "Detector", "Basic") {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddNumberParameter("Width","Width","Šířka obrázku\nWidth of bitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height","Height","Výška obrázku\nHeight of bitmap image", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA) {
            System.Drawing.Bitmap bitmap=null;
            if (!DA.GetData(0, ref bitmap)) { 
                DA.AbortComponentSolution();
                return;
            }
                       
            DA.SetData(0, bitmap.Width);
            DA.SetData(1, bitmap.Height);
        }

        protected override System.Drawing.Bitmap Icon => Properties.Resources.img_size;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C5-4FCA-9839-799AA04BFD58");
    }
}