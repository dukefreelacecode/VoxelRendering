#ifndef OCTREE_NODE_H_
#define OCTREE_NODE_H_

typedef enum {
 fork,
 node
 } octree_node_type;


typedef struct 
{
	octree_node_type tye;
	union 
	{
		void* fork[8];
		float geometry_data[6];
	} data;
 } octree_node;


#endif