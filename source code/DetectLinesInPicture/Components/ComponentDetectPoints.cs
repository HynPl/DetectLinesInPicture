using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace DetectLinesInPicture {
    public class ComponentDetectPoints : GH_Component {
        public ComponentDetectPoints() : base("Detect Points", "Points detection",
            "Description",
            "Detector", "Detecting") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap",  "Bitmap",   "Obrázek ve tvatu èerná=pozadí, bílá=èáryBitmap image", GH_ParamAccess.item);
            pManager.AddNumberParameter("Mode",     "Mode",     "0 fast and unprecise to 1 better quality", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("Ignore",   "Number",   "Ignore low pixel value (1=no ignore .. to .. 255=max ignore)", GH_ParamAccess.item, 50);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddPointParameter("Points", "Points", "Body nalezené", GH_ParamAccess.list);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            // naèíst
            Bitmap bitmapOriginal = null;
            if (!DA.GetData(0, ref bitmapOriginal)) {
                DA.AbortComponentSolution();
                return;
            }
           
            double RawIgnore=50;
            DA.GetData(2, ref RawIgnore);
            int ignore=(int)RawIgnore;
            if (ignore<1)ignore=1;
            if (ignore>255)ignore=255;

            double quality=0.5;
            if (!DA.GetData(1, ref quality)) {
                quality=0.5;
            }
            int PictureWidth=bitmapOriginal.Width;
            int PictureHeight=bitmapOriginal.Height;

            Rectangle rec = new Rectangle(0, 0, PictureWidth, PictureHeight); 
            BitmapData data = bitmapOriginal.LockBits(rec, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            byte* pointer = (byte*)data.Scan0;

            Analyzator.PictureWidth=PictureWidth;
            Analyzator.PictureHeight=PictureHeight;
            Analyzator.Pointer=pointer;
            int min=1;
            int max;
            if (PictureWidth<PictureHeight) max=PictureHeight/2;
            else max=PictureWidth/2;
            Analyzator.Stride=data.Stride;
            Analyzator.NextPoint=(int)(min+(max-min)*(1-quality));

            // Analyzovat obrázek
            List<DInt> PreAnalyzed=Analyzator.Analyze(ignore);

            List<Point3d> pts = new List<Point3d>();
            foreach (DInt pt in PreAnalyzed) {
                pts.Add(new Point3d(pt.X, pt.Y,0));
            }

            bitmapOriginal.UnlockBits(data);
            DA.SetDataList(0, pts);
        }

        protected override Bitmap Icon => Properties.Resources.dot;

        public override Guid ComponentGuid => new Guid("44712965-93F3-43EE-BDC4-3257A082F821");
    }
}