using System;
using System.Collections.Generic;

namespace DetectLinesInPicture {
    internal class MyPolyline { 
        public List<DIntB> Points;

        // Start, End... znamenajó ešli je polyline napojená start = na začátko, end = na konco
        public bool Start { 
            get { 
                if (Points.Count>0) return Points[0].N;
                return false;
            }
            set{ 
                Points[0]=new DIntB(Points[0].X,Points[0].Y,value);
            }
        }
            
        public bool End { 
            get { 
                if (Points.Count>0) return Points[Points.Count-1].N;
                return false;
            }

            set{ 
                  Points[Points.Count-1]=new DIntB(Points[Points.Count-1].X,Points[Points.Count-1].Y,value);
            }
        }

        // Přidat uzavírací bod na konci
        public void AddClosingEndPoint(DIntB pt) { 
            DIntB end=Points[Points.Count-1];
            if (pt.X==end.X && pt.Y==end.Y){ 
                Points[Points.Count-1]=new DIntB(end.X,end.Y,true);
                return;
            }
            pt.N=true;
            Points.Add(pt);
        }

        // Přidat uzavírací bod na začátku
        public void AddClosingStartPoint(DIntB pt) { 
            DIntB start=Points[0];
            if (pt.X==start.X && pt.Y==start.Y){ 
                Points[Points.Count-1]=new DIntB(start.X,start.Y,true);
                return;
            }
            pt.N=true;
            Points.Insert(0,pt);
        }

        public MyPolyline(List<DIntB> points) {
            Points = points;
        }

        // První bod
        public DIntB GetPointStart() => Points[0];

        //Poslední bod
        public DIntB GetPointEnd() => Points[Points.Count-1];

        // Zkus spojit dvě polyline do jedné
        public bool TryMergeWith(MyPolyline p) { 
            if (!Start && !p.Start) {
                Points.Reverse();
                Points.AddRange(p.Points);
                return true;
            }
            return false;
        }

        // Zkus uzavřít polyline
        public bool TryCloseSelf(double limitDistance, int limitMinCount=5) { 
            // nepřopojovat pokud juž je uzavřená
            if (Start || End) return false;

            DIntB start = Points[0];
            DIntB end = Points[Points.Count - 1];
            if (start.X==end.X && start.Y==end.Y) { 
                Points[0]=new DIntB(start.X, start.Y, true);
                return true;
            }

            // Výpočet vzdálenosti
            int dX = start.X - end.X, 
                dY = start.Y - end.Y;
            double dis = Math.Sqrt(dX*dX + dY*dY);

            // Omezení vzdálenosti
            if (Points.Count < limitMinCount) return false;
            
            if (dis <= limitDistance) {

                // Už je uzavřená, ani o tom polyline neví
                if (dis==0) { 
                    Start=true;
                    End=true;
                    return true;
                }

                // Spoj polyline do O
                Points.Add(start);
                Start=true;
                End=true;
                return true;
            }
            return false;
        }

        // Hledání nejbližšího bodu
        public static (MyPolyline, int) NearPointToPoint(List<MyPolyline> polylines, double minDistance, DIntB point, MyPolyline exclude) {
            int ptIndex=-1; // Napojovací bod
            double bestDistance=int.MaxValue;
            MyPolyline bestPolyline=null;

            foreach (MyPolyline pl in polylines) {
                if (pl==exclude) continue;
                for (int i=0; i<pl.Points.Count; i++) {
                    DIntB pt = pl.Points[i];

                    // Výpočet vzdáenosti
                    int dX = pt.X - point.X,
                        dY = pt.Y - point.Y;
                    double dis = Math.Sqrt(dX*dX + dY*dY);

                    // Omezení vzdálenosti
                    if (dis <= minDistance) {
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

        // Nejbližší bod od začátku ve vlatní polyline
        public int NearPointToSelfPointStart(double minDistance, int ignore) {
            int ptIndex=-1; // Napojovací bod
            DIntB start=Points[0];
            double bestDistance=int.MaxValue;

            for (int i=ignore; i<Points.Count; i++){
                DIntB pt = Points[i];

                // Výpočet vzdáenosti
                int dX = pt.X - start.X,
                    dY = pt.Y - start.Y;
                double dis = Math.Sqrt(dX*dX + dY*dY);

                // Omezení vzdálenosti
                if (dis <= minDistance) {
                    if (dis<bestDistance) { 
                        ptIndex=i; 
                        bestDistance=dis; 
                    }
                }
            }
            
            return ptIndex;
        }

        // Nejbližší bod od konce ve vlatní polyline
        public int NearPointToSelfPointEnd(double minDistance, int ignore) {
            DIntB end=GetPointEnd();
            int ptIndex=-1; // Napojovací bod
            double bestDistance=int.MaxValue;

            for (int i=0; i<Points.Count-ignore; i++) {
                DIntB pt = Points[i];

                // Výpočet vzdáenosti
                int dX = pt.X - end.X,
                    dY = pt.Y - end.Y;
                double dis = Math.Sqrt(dX*dX + dY*dY);

                // Omezení vzdálenosti
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
