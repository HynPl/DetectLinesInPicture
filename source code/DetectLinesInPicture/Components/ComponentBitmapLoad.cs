using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.IO;

namespace DetectLinesInPicture {
    public class ComponentBitmapLoad : GH_Component {
        public ComponentBitmapLoad()
          : base("Get load image", "Get load image",
            "Načíst obrázek do Grasshoppru\nLoad image file to Grasshopper",
            "Detector", "Basic") {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager) {
            pManager.AddTextParameter("Text", "Filepath", "Cesta k obrázku v textovém formátu\nFilepath of the bitmap", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager) {
            pManager.AddGenericParameter("Bitmap", "Bitmap", "Obrázek\nBitmap image", GH_ParamAccess.item);
        }

        protected unsafe override void SolveInstance(IGH_DataAccess DA) {
            string file="";
           
            if (!DA.GetData(0, ref file)) { 
                DA.AbortComponentSolution();
                return;
            }
            if (!File.Exists(file)) { 
                DA.AbortComponentSolution();
                return;
            }
            
            Bitmap bitmap;
            try {
                bitmap=(Bitmap)Image.FromFile(file);
            } catch{  
                DA.AbortComponentSolution();
                return;
            }
            if (bitmap.PixelFormat!=System.Drawing.Imaging.PixelFormat.Format24bppRgb) { 
                DA.SetData(0, Convert(bitmap));

                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,"Image was converted to PixelFormat.Format24bppRgb");
                return;
            } else DA.SetData(0, bitmap);
        }

        Bitmap Convert(Bitmap orig){
            return orig.Clone(new Rectangle(0, 0, orig.Width, orig.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            //Bitmap clone = new Bitmap(orig.Width, orig.Height,

            //using (Graphics gr = Graphics.FromImage(clone)) {
            //    gr.DrawImage(orig, new Rectangle(0, 0, clone.Width, clone.Height));
            //} 
            //return clone;
        }

        protected override Bitmap Icon => Properties.Resources.open;

        public override Guid ComponentGuid => new Guid("49D8A02A-30C6-4FCA-9839-799AA04BFD49");
    }
}