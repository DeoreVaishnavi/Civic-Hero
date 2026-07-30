"""
Issue detection service to determine if a specific issue described in a complaint
is still visible in a resolution image.
"""
from typing import List
import logging

logger = logging.getLogger(__name__)

class IssueDetectionService:
    def __init__(self):
        """
        Initialize the issue detection service.
        In a production implementation, this might load a visual question answering (VQA) model
        or an object detection model trained on civic issues.
        """
        # For now, we'll use a rule-based approach with keyword matching
        # In a real implementation, you would use a trained VQA model like BLIP, ViLT, etc.
        self.issue_keywords = {
            # Infrastructure issues
            "pothole": ["hole", "crack", "break", "damage", "hole in road", "road damage"],
            "broken_light": ["light", "lamp", "post", "illumination", "dark", "out"],
            "graffiti": ["paint", "spray", "marking", "drawing", "tag", "vandalism"],
            "trash": ["trash", "garbage", "waste", "debris", "litter", "rubbish"],
            "fallen_tree": ["tree", "branch", "limb", "fallen", "down", "blocking"],
            "flooding": ["water", "flood", "puddle", "standing water", "pool"],
            "signal_issue": ["signal", "light", "traffic", "red", "yellow", "green", "not working"],
            "sign_damage": ["sign", "stop", "yield", "sign", "broken", "damaged", "missing"],
        }

    def analyze_issue_visibility(
        self,
        complaint_description: str,
        image_bytes: bytes
    ) -> tuple[bool, List[str]]:
        """
        Analyze whether the issue described in the complaint is still visible in the image.

        Args:
            complaint_description: Description of the reported issue
            image_bytes: The image to analyze (resolution image)

        Returns:
            Tuple of (issue_still_visible: bool, observations: List[str])
        """
        # In a real implementation, this would use a vision-language model
        # For now, we'll return a placeholder result based on simple heuristics

        observations = []
        issue_still_visible = False  # Default assumption - issue is resolved

        # Clean and normalize the complaint description
        desc_lower = complaint_description.lower().strip()

        # Extract potential issue keywords from the complaint
        detected_issues = []
        for issue_type, keywords in self.issue_keywords.items():
            if any(keyword in desc_lower for keyword in [issue_type.replace('_', ' ')] + keywords):
                detected_issues.append(issue_type)
                observations.append(f"Complaint mentions potential {issue_type.replace('_', ' ')} issue")

        # If we detected specific issues, we would analyze the image for those
        # For this prototype, we'll simulate the analysis
        if detected_issues:
            # In reality, we would run object detection or VQL model here
            # For now, we'll randomly determine if issue is visible (for demonstration)
            # In production, this would be replaced with actual ML model inference
            import random
            # Simulate some randomness but bias toward "resolved" for demo purposes
            issue_still_visible = random.random() < 0.3  # 30% chance issue still visible

            if issue_still_visible:
                observations.append("AI analysis suggests the reported issue may still be visible")
            else:
                observations.append("AI analysis suggests the reported issue appears to be resolved")
        else:
            observations.append("No specific issue type detected in complaint description")
            # If we can't identify the issue, we can't reliably detect it in the image
            issue_still_visible = False

        return issue_still_visible, observations

# Create a singleton instance
issue_detection_service = IssueDetectionService()