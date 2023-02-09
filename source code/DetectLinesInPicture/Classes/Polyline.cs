using System;
using System.Collections;
using System.Collections.Generic;

namespace DetectLinesInPicture {
    internal class MyPolyline { 
        public List<DIntB> Points;

        // Start, End... znamenajó ešli je polyline napojená start = na začátko, end = na konco
        public bool Start{ 
            get { 
                if (Points.Count>0) return Points[0].N;
                return false;
            }
            set{ 
                Points[0]=new DIntB(Points[0].X,Points[0].Y,value);
               // if (Points.Count>0) Points[0].SetN(value);
            }
        }
            
        public bool End{ 
            get { 
                if (Points.Count>0) return Points[Points.Count-1].N;
                return false;
            }

            set{ 
             //   if (Points.Count>0) Points[Points.Count-1].SetN(value);
                  Points[Points.Count-1]=new DIntB(Points[Points.Count-1].X,Points[Points.Count-1].Y,value);
            }
        }

        public void AddClosingEndPoint(DIntB pt) { 
            DIntB end=Points[Points.Count-1];
            if (pt.X==end.X && pt.Y==end.Y){ 
                Points[Points.Count-1]=new DIntB(end.X,end.Y,true);
              //  Points[Points.Count-1].SetN(true);
                return;
            }
            pt.N=true;
            Points.Add(pt);
        }

        public void AddClosingStartPoint(DIntB pt) { 
            DIntB start=Points[0];
            if (pt.X==start.X && pt.Y==start.Y){ 
                Points[Points.Count-1]=new DIntB(start.X,start.Y,true);
               // Points[0].SetN(true);
                return;
            }
            pt.N=true;
            Points.Insert(0,pt);
        }

        public MyPolyline(List<DIntB> points/*, bool start, bool end*/) {
            Points = points;
           // Start = Points[0].N;//start;
           // End = Points[Points.Count-1].N;//end;
        }

        public DIntB GetPointStart() => Points[0];
        public DIntB GetPointEnd() => Points[Points.Count-1];

        public bool TryMergeWith(MyPolyline p) { 
            if (!Start && !p.Start) {
                Points.Reverse();
                Points.AddRange(p.Points);
                return true;
            }
            return false;
        }

        public bool TryCloseSelf(double limitDistance, int limitMinCount=5) { 
            // Check start and ending point, connect then
            if (Start || End) return false;

            DIntB start = Points[0];
            DIntB end = Points[Points.Count - 1];
            if (start.X==end.X && start.Y==end.Y){ 
                //Start=true;
                //End=true;
                Points[0]=new DIntB(start.X,start.Y,true);//.N=true;
                return true;
            }
            int dX = start.X - end.X, 
                dY = start.Y - end.Y;
            double dis = Math.Sqrt(dX*dX + dY*dY);

            if (Points.Count < limitMinCount) return false;
            
            if (dis <= limitDistance) {
                if (dis==0){ 
                    Start=true;
                    End=true;
                    return true;
                }
                Points.Add(start);
                Start=true;
                End=true;
                return true;
            }
            return false;
        }

        // hledání nejbližšího bodu
        public static (MyPolyline, int) NearPointToPoint(List<MyPolyline> polylines, double minDistance, DIntB point, MyPolyline exclude) {
            int ptIndex=-1;
            double bestDistance=int.MaxValue;
            MyPolyline bestPolyline=null;

            foreach (MyPolyline pl in polylines) {
                if (pl==exclude) continue;
                for (int i=0; i<pl.Points.Count; i++) {
                    DIntB pt = pl.Points[i];

                    int dX = pt.X - point.X,
                        dY = pt.Y - point.Y;
                    double dis = Math.Sqrt(dX*dX + dY*dY);

                    if (dis <= minDistance/* && dis!=0*/) {
                        if (dis<bestDistance) { 
                            ptIndex=i; 
                            bestDistance=dis; 
                            bestPolyline=pl;
                        }
                    } 
                }
            }
            
            return (bestPolyline, ptIndex);
        }

        public int NearPointToSelfPointStart(double minDistance, int ignore) {
            DIntB start=Points[0];

            int ptIndex=-1;
            double bestDistance=int.MaxValue;

            for (int i=ignore; i<Points.Count; i++){
                DIntB pt = Points[i];

                int dX = pt.X - start.X,
                    dY = pt.Y - start.Y;
                double dis = Math.Sqrt(dX*dX + dY*dY);

                if (dis <= minDistance) {
                    if (dis<bestDistance) { 
                        ptIndex=i; 
                        bestDistance=dis; 
                    }
                }
            }
            
            return ptIndex;
        }

        public int NearPointToSelfPointEnd(double minDistance, int ignore) {
            DIntB end=GetPointEnd();

            int ptIndex=-1;
            double bestDistance=int.MaxValue;

            for (int i=0; i<Points.Count-ignore; i++) {
                DIntB pt = Points[i];

                int dX = pt.X - end.X,
                    dY = pt.Y - end.Y;
                double dis = Math.Sqrt(dX*dX + dY*dY);

                if (dis <= minDistance) {
                    if (dis<bestDistance) { 
                        ptIndex=i; 
                        bestDistance=dis; 
                    }
                }
            }
            
            return ptIndex;
        }
    }
}
