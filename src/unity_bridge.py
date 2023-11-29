#!/usr/bin/env python3

import rospy
import numpy as np

from auv_msgs.msg import ThrusterForces, DeadReckonReport, UnityState
from geometry_msgs.msg import Pose, Twist 
from sbg_driver.msg import SbgImuData, SbgEkfQuat
from std_msgs.msg import Float64

def cb_unity_state(msg):
    pass


if __name__ == '__main__':
    rospy.init_node('unity_bridge')

    rospy.Subscriber('/unity/state', Pose, cb_unity_state)

    pub_dvl_sensor = rospy.Publisher('dead_reckon_report', DeadReckonReport, queue_size=1)
    pub_depth_sensor = rospy.Publisher('depth', Float64, queue_size=1)
    pub_imu_quat_sensor = rospy.Publisher('sbg/ekf_quat', SbgEkfQuat, queue_size=1)
    pub_imu_data_sensor = rospy.Publisher('sbg/imu_data', SbgImuData, queue_size=1)