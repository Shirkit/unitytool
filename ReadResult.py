import copy
import math
import pylab
from mpl_toolkits.axes_grid1 import host_subplot
import mpl_toolkits.axisartist as AA
import matplotlib.pyplot as plt

o = open("pathresults.xml")

paths = [[]]*2

r = dict()

p = -1
t = 0

w = 0

for i, l in enumerate(o.readlines()):

	if "name=" in l and "Path" in l:
		p+=1
		t=0
		w = 0
	if "time=" in l:
		t+=1

	if("Crazyness" in l):
		#clear
		l=l.replace("<metric name=\"Crazyness\">","")
		l=l.replace("</metric>","")
		l=l.replace("\n","")

		r["Crazyness"] = float(l)
	if("Danger3\">" in l):
		#clear
		l=l.replace("<metric name=\"Danger3\">","")
		l=l.replace("</metric>","")
		l=l.replace("\n","")

		r["Danger3"] = float(l)
	if("Los3\">" in l):
		#clear
		l=l.replace("<metric name=\"Los3\">","")
		l=l.replace("</metric>","")
		l=l.replace("\n","")

		r["Los3"] = float(l)

	if("</results>" in l):
		#print w
		w+=1
		#print p
		paths[p].append(copy.deepcopy(r))


#Plot the restults
x = [] ;yD = []; yL = [];yC=[]




for i,j in enumerate(paths[0]):
	#print i 
	x.append(i)
	yD.append(j["Danger3"])
	yL.append(j["Los3"])
	yC.append(j["Crazyness"])

host = host_subplot(111, axes_class=AA.Axes)
plt.subplots_adjust(right=0.75)

par1 = host.twinx()
par2 = host.twinx()

offset = 70
new_fixed_axis = par2.get_grid_helper().new_fixed_axis
par2.axis["right"] = new_fixed_axis(loc="right",
                                        axes=par2,
                                        offset=(offset, 0))
        
par2.axis["right"].toggle(all=True)

host.set_xlabel("Time")
host.set_ylabel("Danger")
par1.set_ylabel("Crazyness")
par2.set_ylabel("LOS")

p1, = host.plot(x,yD, label="Danger")
p2, = par1.plot(x,yC, label="Crazy")
p3, = par2.plot(x,yL, label="LOS")
#host.legend()

host.axis["left"].label.set_color(p1.get_color())
par1.axis["right"].label.set_color(p2.get_color())
par2.axis["right"].label.set_color(p3.get_color())

plt.draw()
#plt.show()
#nameFile = nameFile.replace(".xml","")
plt.savefig("Output.svg", format = "svg")