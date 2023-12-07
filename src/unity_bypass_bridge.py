#!/usr/bin/env python3

import rospy
import numpy as np
import quaternion
from tf import transformations
from tf2_ros import TransformBroadcaster
from auv_msgs.msg import UnityState
from geometry_msgs.msg import Pose, Quaternion, Vector3, TransformStamped, Point
from std_msgs.msg import Float64, Bool

DEG_PER_RAD = 180 / np.pi
q_NED_NWU = np.quaternion(0, 1, 0, 0)

def cb_unity_state(msg):
    pose_x = msg.position.x
    pose_y = -msg.position.y
    pose_z = -msg.position.z
    
    pose_theta_x = -msg.eulerAngles.y
    pose_theta_y = -msg.eulerAngles.x
    pose_theta_z = -msg.eulerAngles.z

    twist_linear_x = msg.velocity.x
    twist_linear_y = -msg.velocity.y
    twist_linear_z = -msg.velocity.z
    twist_angular_x = -msg.angular_velocity.x
    twist_angular_y = -msg.angular_velocity.y
    twist_angular_z = msg.angular_velocity.z
    
    pub_x.publish(pose_x)
    pub_y.publish(pose_y)
    pub_z.publish(pose_z)
    
    q_NWU_auv = transformations.quaternion_from_euler(pose_theta_x/DEG_PER_RAD, pose_theta_y/DEG_PER_RAD, pose_theta_z/DEG_PER_RAD)
   
    pub_theta_x.publish(pose_theta_x)
    pub_theta_y.publish(pose_theta_y)
    pub_theta_z.publish(pose_theta_z)
    
    pub_ang_vel.publish(Vector3(twist_angular_x, twist_angular_y, twist_angular_z))
    pub_lin_vel.publish(Vector3(twist_linear_x, twist_linear_y, twist_linear_z))
    
    pose = Pose(Point(x=pose_x, y=pose_y, z=pose_z), Quaternion(x = q_NWU_auv[0], y = q_NWU_auv[1], z = q_NWU_auv[2], w = q_NWU_auv[3]))
    pub_pose.publish(pose)
    broadcast_auv_pose(pose)
    
    pub_imu_sensor_status.publish(Bool(True))
    pub_depth_sensor_status.publish(Bool(True))
    pub_dvl_sensor_status.publish(Bool(True))

def broadcast_auv_pose(pose):
    t = TransformStamped()
    t.header.stamp = rospy.Time.now()
    t.header.frame_id = "world"
    t.child_frame_id = "auv_base"
    t.transform.translation.x = pose.position.x
    t.transform.translation.y = pose.position.y
    t.transform.translation.z = pose.position.z 
    t.transform.rotation = pose.orientation
    tf_broadcaster.sendTransform(t)

    t_rot = TransformStamped()
    t_rot.header.stamp = rospy.Time.now()
    t_rot.header.frame_id = "world_rotation"
    t_rot.child_frame_id = "auv_rotation"
    t_rot.transform.translation.x = 0
    t_rot.transform.translation.y = 0
    t_rot.transform.translation.z = 0
    t_rot.transform.rotation = pose.orientation
    tf_broadcaster.sendTransform(t_rot)



if __name__ == '__main__':
    rospy.init_node('unity_bypass_bridge')

    # Set up subscribers and publishers

    pub_imu_sensor_status = rospy.Publisher("/sensors/imu/status", Bool, queue_size=1)
    pub_depth_sensor_status = rospy.Publisher("/sensors/depth/status", Bool, queue_size=1)
    pub_dvl_sensor_status = rospy.Publisher("/sensors/dvl/status", Bool, queue_size=1)
    pub_pose = rospy.Publisher('/state/pose', Pose, queue_size=1)
    pub_x = rospy.Publisher('/state/x', Float64, queue_size=1)
    pub_y = rospy.Publisher('/state/y', Float64, queue_size=1)
    pub_z = rospy.Publisher('/state/z', Float64, queue_size=1)
    pub_theta_x = rospy.Publisher('/state/theta/x', Float64, queue_size=1)
    pub_theta_y = rospy.Publisher('/state/theta/y', Float64, queue_size=1)
    pub_theta_z = rospy.Publisher('/state/theta/z', Float64, queue_size=1)
    pub_ang_vel = rospy.Publisher('/state/angular_velocity', Vector3, queue_size=1)
    pub_lin_vel = rospy.Publisher('/state/linear_velocity', Vector3, queue_size=1)
    tf_broadcaster = TransformBroadcaster()
 
    rospy.Subscriber('/unity/state', UnityState, cb_unity_state)
    
    rospy.spin()