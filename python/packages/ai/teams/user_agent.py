"""
Copyright (c) Microsoft Corporation. All rights reserved.
Licensed under the MIT License.
"""

from importlib.metadata import version


class _UserAgent:
    @property
    def user_agent(self) -> str:
        return f"teamsai-py/{version('teams-ai')}"
