import numpy 
import pylab 
from scipy.stats import norm
from numpy import linspace
from pylab import plot,show,hist,figure,title
import matplotlib.pyplot as plt
import numpy as np
import csv

dataSuccess = dict()
dataTime = dict()
dataFrames = dict()
dataKey = dict()
dataStates = dict()

with open("Exag1-test2.csv","rb") as csvfile:
	reader = csv.reader(csvfile)
	for i, row in enumerate(reader):
		
		if i == 0:
			continue
		
		if str(row[0]) in dataSuccess:
			dataSuccess[str(row[0])].append(float(row[2]))
			dataTime[str(row[0])].append(float(row[3]))
			dataFrames[str(row[0])].append(float(row[4]))
			dataKey[str(row[0])].append(float(row[5]))
			dataStates[str(row[0])].append(float(row[6]))

		else:
			dataSuccess[str(row[0])] = []
			dataTime[str(row[0])] = []
			dataFrames[str(row[0])] = []
			dataKey[str(row[0])] = []
			dataStates[str(row[0])] = []

			dataSuccess[str(row[0])].append(float(row[2]))
			dataTime[str(row[0])].append(float(row[3]))
			dataFrames[str(row[0])].append(float(row[4]))
			dataKey[str(row[0])].append(float(row[5]))
			dataStates[str(row[0])].append(float(row[6]))
		
mu, std = norm.fit(dataKey["AStar2"])

names = {"AStar2","AStar3","UCT","RRTASTAR","RRTUCT"}
#names = {"AStar2","AStar3","UCT","RRTASTAR","RRTMCT"}
namesValue = {"AStar2":"\\Astar$_2$","AStar3":"\\Astar$_3$","UCT":"MCTS","RRTASTAR":"RRT$_{\\mbox{\Astar}}$","RRTUCT":"RRT$_{\\mbox{MCTS}}$"}

#Header for the table


for name in names:

	#Clean the data
	muTimeAll, stdTimeAll = norm.fit(dataTime[name])

	#Time All Search
	print  namesValue[name] +" & " + "{:1.1f}".format(muTimeAll) + "  $\pm$  " + "{:1.1f}".format(stdTimeAll) 


	#Success Rate
	successCount = 0
	toRemove = []
	for i, v in enumerate(dataSuccess[name]):
		if v == 1:
			successCount +=1
		else:
			toRemove.append(i)
	#clean
	for i in reversed(toRemove):

		del dataTime[name][i]
		del dataKey[name][i]
		del dataFrames[name][i]
		del dataStates[name][i]

	##Check if list empty
	if len(dataTime[name]) == 0:
		dataTime[name].append(0)
	if len(dataKey[name]) == 0:
		dataKey[name].append(0)
	if len(dataFrames[name]) == 0:
		dataFrames[name].append(0)
	if len(dataStates[name]) == 0:
		dataStates[name].append(0)

	print " & " + "{:1.2f}".format(float(successCount)/float(len(dataSuccess[name]))) 

	#Success time
	mu, std = norm.fit(dataTime[name])
	print  " & " + "{:1.1f}".format(mu) + "  $\pm$  " + "{:1.1f}".format(std) 
	#Success Keys
	mu, std = norm.fit(dataFrames[name])
	print  " & " + "{:1.1f}".format(mu) + "  $\pm$  " + "{:1.1f}".format(std) 
	#Success Keys Count
	mu, std = norm.fit(dataKey[name])
	print  " & " + "{:1.1f}".format(mu) + "  $\pm$  " + "{:1.1f}".format(std) 
	#Success States Count
	mu, std = norm.fit(dataStates[name])
	print  " & " + "{:1.0f}".format(mu) + "  $\pm$  " + "{:1.0f}".format(std)
	print '\\' + '\\' 