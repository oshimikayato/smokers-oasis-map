"""
Object Recognition Module
Uses OpenCV's ORB (Oriented FAST and Rotated BRIEF) for object recognition.
Allows users to register custom objects and identify them later.
"""

import cv2
import numpy as np
import os
import json
from datetime import datetime

class ObjectRecognizer:
    def __init__(self, data_dir="object_data"):
        self.data_dir = data_dir
        self.objects_json = os.path.join(data_dir, "objects.json")
        
        # Create data directory if not exists
        if not os.path.exists(data_dir):
            os.makedirs(data_dir)
        
        # ORB detector (good balance of speed and accuracy)
        self.orb = cv2.ORB_create(nfeatures=1000)
        
        # FLANN matcher for faster matching
        FLANN_INDEX_LSH = 6
        index_params = dict(algorithm=FLANN_INDEX_LSH,
                           table_number=6,
                           key_size=12,
                           multi_probe_level=1)
        search_params = dict(checks=50)
        self.matcher = cv2.FlannBasedMatcher(index_params, search_params)
        
        # BFMatcher as fallback
        self.bf_matcher = cv2.BFMatcher(cv2.NORM_HAMMING, crossCheck=False)
        
        # Object data: {name: {"descriptors_path": "...", "keypoints_count": N, "registered_at": "..."}}
        self.objects = {}
        
        # Cached descriptors for faster matching
        self.cached_descriptors = {}
        
        # Load existing data
        self._load_data()
    
    def _load_data(self):
        """Load saved object data"""
        if os.path.exists(self.objects_json):
            with open(self.objects_json, 'r', encoding='utf-8') as f:
                self.objects = json.load(f)
            
            # Load descriptors into cache
            for name, info in self.objects.items():
                desc_path = os.path.join(self.data_dir, f"{name}_descriptors.npy")
                if os.path.exists(desc_path):
                    self.cached_descriptors[name] = np.load(desc_path)
            
            print(f"[ObjectRecognizer] Loaded {len(self.objects)} registered objects")
    
    def _save_data(self):
        """Save object data"""
        with open(self.objects_json, 'w', encoding='utf-8') as f:
            json.dump(self.objects, f, ensure_ascii=False, indent=2)
    
    def register_object(self, image, name):
        """
        Register an object with a name.
        Extracts ORB features and saves them.
        Returns: {"success": True/False, "message": "..."}
        """
        if image is None:
            return {"success": False, "message": "Invalid image"}
        
        if not name or len(name.strip()) == 0:
            return {"success": False, "message": "Name is required"}
        
        name = name.strip()
        
        # Convert to grayscale
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        
        # Enhance contrast
        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        gray = clahe.apply(gray)
        
        # Detect keypoints and compute descriptors
        keypoints, descriptors = self.orb.detectAndCompute(gray, None)
        
        if descriptors is None or len(keypoints) < 10:
            return {"success": False, "message": "Not enough features detected. Try a more textured object or different angle."}
        
        # Save descriptors
        desc_path = os.path.join(self.data_dir, f"{name}_descriptors.npy")
        np.save(desc_path, descriptors)
        
        # Save thumbnail
        thumb_path = os.path.join(self.data_dir, f"{name}_thumbnail.jpg")
        thumbnail = cv2.resize(image, (100, 100))
        cv2.imwrite(thumb_path, thumbnail)
        
        # Update object info
        self.objects[name] = {
            "keypoints_count": len(keypoints),
            "descriptors_count": len(descriptors),
            "registered_at": datetime.now().isoformat()
        }
        
        # Update cache
        self.cached_descriptors[name] = descriptors
        
        # Save to file
        self._save_data()
        
        print(f"[ObjectRecognizer] Registered object '{name}' with {len(keypoints)} keypoints")
        
        return {
            "success": True,
            "message": f"Object '{name}' registered successfully with {len(keypoints)} features",
            "name": name,
            "features": len(keypoints)
        }
    
    def add_sample_to_object(self, image, name):
        """
        Add a sample image to an existing object or create new one.
        Used for multi-image registration (e.g., 30 images from different angles).
        Returns: {"success": True/False, "message": "...", "current_features": N}
        """
        if image is None:
            return {"success": False, "message": "Invalid image", "current_features": 0}
        
        if not name or len(name.strip()) == 0:
            return {"success": False, "message": "Name is required", "current_features": 0}
        
        name = name.strip()
        
        # Convert to grayscale
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        
        # Enhance contrast
        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        gray = clahe.apply(gray)
        
        # Detect keypoints and compute descriptors
        keypoints, new_descriptors = self.orb.detectAndCompute(gray, None)
        
        if new_descriptors is None or len(keypoints) < 5:
            return {"success": False, "message": "Not enough features in this frame", "current_features": 0}
        
        # Check if object already has descriptors
        desc_path = os.path.join(self.data_dir, f"{name}_descriptors.npy")
        
        if name in self.cached_descriptors:
            # Append new descriptors to existing ones
            existing_desc = self.cached_descriptors[name]
            combined_desc = np.vstack([existing_desc, new_descriptors])
            
            # Limit total descriptors to prevent memory issues (max 30000)
            if len(combined_desc) > 30000:
                combined_desc = combined_desc[-30000:]
            
            np.save(desc_path, combined_desc)
            self.cached_descriptors[name] = combined_desc
            total_features = len(combined_desc)
        else:
            # First sample - save as is
            np.save(desc_path, new_descriptors)
            self.cached_descriptors[name] = new_descriptors
            total_features = len(new_descriptors)
            
            # Save first frame as thumbnail
            thumb_path = os.path.join(self.data_dir, f"{name}_thumbnail.jpg")
            thumbnail = cv2.resize(image, (100, 100))
            cv2.imwrite(thumb_path, thumbnail)
        
        # Update object info
        self.objects[name] = {
            "keypoints_count": total_features,
            "descriptors_count": total_features,
            "registered_at": datetime.now().isoformat()
        }
        
        # Save metadata
        self._save_data()
        
        print(f"[ObjectRecognizer] Added sample to '{name}': +{len(new_descriptors)} features (total: {total_features})")
        
        return {
            "success": True,
            "message": f"Added {len(new_descriptors)} features",
            "current_features": total_features,
            "added_features": len(new_descriptors)
        }
    
    def identify_objects(self, image, min_matches=15, ratio_threshold=0.75):
        """
        Identify registered objects in an image.
        Uses ratio test for robust matching.
        Returns: {"success": True, "objects": [{"name": "...", "confidence": ...}, ...]}
        """
        if image is None:
            return {"success": False, "message": "Invalid image", "objects": []}
        
        if len(self.cached_descriptors) == 0:
            return {"success": True, "message": "No objects registered", "objects": []}
        
        # Convert to grayscale
        gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
        
        # Enhance contrast
        clahe = cv2.createCLAHE(clipLimit=2.0, tileGridSize=(8, 8))
        gray = clahe.apply(gray)
        
        # Detect keypoints and compute descriptors
        keypoints, descriptors = self.orb.detectAndCompute(gray, None)
        
        if descriptors is None or len(keypoints) < 5:
            return {"success": True, "message": "Not enough features in image", "objects": []}
        
        detected_objects = []
        
        for name, stored_desc in self.cached_descriptors.items():
            try:
                # Use BFMatcher with knnMatch
                matches = self.bf_matcher.knnMatch(stored_desc, descriptors, k=2)
                
                # Apply ratio test (Lowe's ratio test)
                good_matches = []
                for match in matches:
                    if len(match) == 2:
                        m, n = match
                        if m.distance < ratio_threshold * n.distance:
                            good_matches.append(m)
                
                # Calculate confidence based on number of good matches
                if len(good_matches) >= min_matches:
                    # Confidence: ratio of good matches to total stored features
                    confidence = min(1.0, len(good_matches) / (min_matches * 2))
                    detected_objects.append({
                        "name": name,
                        "matches": len(good_matches),
                        "confidence": round(confidence, 2)
                    })
                    print(f"[ObjectRecognizer] Detected '{name}' with {len(good_matches)} matches (conf: {confidence:.2f})")
            
            except Exception as e:
                print(f"[ObjectRecognizer] Error matching {name}: {e}")
                continue
        
        # Sort by confidence
        detected_objects.sort(key=lambda x: x["confidence"], reverse=True)
        
        return {
            "success": True,
            "message": f"Found {len(detected_objects)} objects",
            "objects": detected_objects
        }
    
    def list_objects(self):
        """
        List all registered objects.
        Returns: {"success": True, "objects": [...]}
        """
        objects_list = []
        for name, info in self.objects.items():
            objects_list.append({
                "name": name,
                "keypoints": info.get("keypoints_count", 0),
                "registered_at": info.get("registered_at", "")
            })
        
        # All 80 YOLO/COCO classes
        coco_classes = [
            "person", "bicycle", "car", "motorcycle", "airplane",
            "bus", "train", "truck", "boat", "traffic light",
            "fire hydrant", "stop sign", "parking meter", "bench", "bird",
            "cat", "dog", "horse", "sheep", "cow",
            "elephant", "bear", "zebra", "giraffe", "backpack",
            "umbrella", "handbag", "tie", "suitcase", "frisbee",
            "skis", "snowboard", "sports ball", "kite", "baseball bat",
            "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle",
            "wine glass", "cup", "fork", "knife", "spoon",
            "bowl", "banana", "apple", "sandwich", "orange",
            "broccoli", "carrot", "hot dog", "pizza", "donut",
            "cake", "chair", "couch", "potted plant", "bed",
            "dining table", "toilet", "tv", "laptop", "mouse",
            "remote", "keyboard", "cell phone", "microwave", "oven",
            "toaster", "sink", "refrigerator", "book", "clock",
            "vase", "scissors", "teddy bear", "hair drier", "toothbrush"
        ]
        
        for name in coco_classes:
            # Avoid duplicates if user manually registered same name
            if name not in self.objects:
                objects_list.append({
                    "name": name,
                    "keypoints": 999, # Dummy value for pre-installed
                    "registered_at": "YOLO"
                })
        
        return {
            "success": True,
            "objects": objects_list,
            "count": len(objects_list)
        }
    
    def delete_object(self, name):
        """
        Delete a registered object.
        Returns: {"success": True/False, "message": "..."}
        """
        if name not in self.objects:
            return {"success": False, "message": f"Object '{name}' not found"}
        
        # Delete files
        desc_path = os.path.join(self.data_dir, f"{name}_descriptors.npy")
        thumb_path = os.path.join(self.data_dir, f"{name}_thumbnail.jpg")
        
        if os.path.exists(desc_path):
            os.remove(desc_path)
        if os.path.exists(thumb_path):
            os.remove(thumb_path)
        
        # Remove from data
        del self.objects[name]
        if name in self.cached_descriptors:
            del self.cached_descriptors[name]
        
        # Save
        self._save_data()
        
        print(f"[ObjectRecognizer] Deleted object '{name}'")
        
        return {"success": True, "message": f"Object '{name}' deleted"}
    
    def get_registered_names(self):
        """Get list of registered object names"""
        return list(self.objects.keys())


# Test code
if __name__ == "__main__":
    recognizer = ObjectRecognizer()
    print("ObjectRecognizer initialized")
    print(f"Registered objects: {recognizer.get_registered_names()}")
